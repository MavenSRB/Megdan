using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Sabina's starting-hand cards played the way a player plays them, from the engaged staging - Wasp
    /// and Sabina two spaces apart around one Blue melee minion, with a Red ranged minion beside her.
    /// Milestone 8.22.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the walk that puts her where a printed sentence can reach, the enemy's own turn, the enemy's own
    /// Defense - ride the headless command surface, exactly as milestones 8.15, 8.20a and 8.21 do, and
    /// are marked where they appear.</para>
    ///
    /// <para><b>One turn is one action, and that is what shapes these journeys.</b> Spending the
    /// secondary Movement ends the turn - the stop control and the end-turn control are the same
    /// command - so a face whose printed sentence needs Sabina somewhere else spends a previous turn
    /// walking there. That is why the two Attack journeys below run over two turns and why the walk is
    /// driven headlessly: the walk is somebody else's regression, and
    /// <see cref="SabinaUiJourneyPlayModeTests.Sabina_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace"/>
    /// is where a player makes it.</para>
    ///
    /// <para><b>The staged board is what makes these cards reachable.</b> The Blue melee minion the
    /// staging engages both heroes around is <em>friendly</em> to Sabina, which is what Back to Back
    /// swaps her with and what Troop Movement orders; the Red ranged minion standing beside her is the
    /// second piece the swap pair exchanges. Neither is arranged by a fixture - both are where p.6's
    /// setup and milestone 8.15's restaging put them.</para>
    /// </summary>
    public sealed class SabinaEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Point Blank Shot: "Target a unit in range. After the attack: If the target is adjacent to
        /// you, push it 1 space."
        ///
        /// <para>The after-attack half is milestone 8.22's present-tense reading, and what makes it
        /// worth a journey is that it asks nobody anything: the push is a fixed one-space displacement
        /// of the piece the Attack already named. What a player sees is the enemy's token standing one
        /// space further away than it was.</para>
        ///
        /// <para>The target is the enemy <em>hero</em> rather than a minion, and it has to be: the card
        /// prints Range 1, a targeted enemy minion is defeated and removed at rulebook p.14's step 6,
        /// and a removed target is not adjacent to anybody. A hero who answers the Defense is the one
        /// target this rider can still be true of - which is exactly the difference from Shootout's
        /// past-tense sibling.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PointBlankShot_ShootsTheAdjacentHeroAndPushesHerOneSpaceAway()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return StepIntoContact(root, sabinaId, enemyId, SabinaCardContent.TroopMovement);
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.PointBlankShot);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                "Point Blank Shot's printed Initiative 12 acts before the lowest card the enemy holds.");

            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            var before = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(before, sabinaAt), Is.EqualTo(1),
                "The walked turn put her beside the enemy hero, which is the board Range 1 needs.");

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question.");
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(enemyId),
                "The enemy hero standing beside her is inside the printed Range 1.");
            var enemyView = HeroView(enemyId);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, enemyView.transform.position), Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;

            // The enemy's own Defense is setup: this journey's subject is what the rider then does.
            // The face chosen is the one the prompt itself says leaves her standing, read off the
            // breakdown a player is shown rather than off a hard-coded number.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "A hero target opens p.14's Defense response. " + Remaining(runtime));
            yield return DefendSuccessfully(root);

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "The enemy answered the Attack Total, so she is still on the board for the printed " +
                "rider to be about.");
            var after = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            Assert.That(after, Is.Not.EqualTo(before),
                "\"If the target is adjacent to you, push it 1 space\" - the target survived and is " +
                "still adjacent, so the push applied. " + Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(after, sabinaAt), Is.EqualTo(2),
                "One space, directly away: p.19's push increases the distance from the pusher by " +
                "exactly the pushed distance.");
            Assert.That(HeroView(enemyId).Coordinate, Is.EqualTo(after),
                "and the token followed her there rather than staying where she was shot.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate,
                Is.EqualTo(sabinaAt),
                "A push moves the target and never the pusher.");

            yield return CaptureGameView("point-blank-shot-pushes-the-hero-it-shot");
        }

        /// <summary>
        /// Back to Back: "Swap with a friendly minion in radius."
        ///
        /// <para>One board click on the miniature, and both pieces change places. The candidates are
        /// the friendly minions inside the printed Radius 2 and nothing else - the enemy standing just
        /// as close is not offered, which is the half of the sentence a player can see by what lights
        /// up.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BackToBack_SwapsSabinaWithTheFriendlyMinionSheClicks()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.BackToBack);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                "Back to Back's printed Initiative 8 acts before the lowest card the enemy holds.");
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed Skill must stop and ask which friendly minion she swaps with. " +
                Remaining(runtime));
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Is.Not.Empty,
                "A minion selection is answered on the map, never as a prompt list. " +
                Remaining(runtime));
            var offered = MinionsAt(candidates.Select(candidate => candidate.Space));
            Assert.That(offered.All(minion => minion.Occupancy.TeamSide ==
                    runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).TeamSide),
                Is.True,
                "\"a friendly minion\" - no enemy piece is offered, however close it stands. " +
                "Offered: " + string.Join(", ", offered.Select(view =>
                    view.Occupancy.MinionId.Value + "/" + view.Occupancy.TeamSide)));

            var partner = offered[0];
            var partnerId = partner.Occupancy.MinionId;
            var partnerAt = partner.Occupancy.Coordinate;
            var swapNode = StandingNode(runtime);
            Assert.That(partner.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(Tile(partnerAt).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Ally),
                "The friendly swap partner uses the warm teammate corners where it stands.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, partner.transform.position), Is.True,
                "The swap partner is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return ConfirmIfStillStanding(root, swapNode);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "One board click answers the whole card. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate,
                Is.EqualTo(partnerAt),
                "Sabina is standing where the minion stood.");
            Assert.That(MinionView(partnerId).Occupancy.Coordinate, Is.EqualTo(startedAt),
                "and the minion is standing where she was - a swap, not a move.");
            Assert.That(HeroView(sabinaId).Coordinate, Is.EqualTo(partnerAt),
                "with her token drawn on the space she exchanged into.");
        }

        /// <summary>
        /// Listen Up: "Swap two minions in radius."
        ///
        /// <para>Milestone 8.19b's target pair, reached by printed content for the first time. It is
        /// one node asking for two pieces, so a player clicks twice on the board and the exchange
        /// applies when the second click lands - and unlike Back to Back the acting hero is not one of
        /// the two, so she does not move at all.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ListenUp_SwapsTheTwoMinionsItsTwoBoardClicksName()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.ListenUp);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                "Listen Up's printed Initiative 9 acts before the lowest card the enemy holds.");
            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed Skill must stop and ask which two minions change places. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(2),
                "\"two minions\" is one selection taking two names rather than two questions. " +
                Remaining(runtime));
            var offered = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(offered.Length, Is.GreaterThanOrEqualTo(2),
                "The printed radius has to hold two miniatures for the card to do anything. " +
                "Offered: " + string.Join(", ", offered.Select(view =>
                    view.Occupancy.MinionId.Value)));

            var pairNode = StandingNode(runtime);
            var first = offered[0];
            var second = offered[1];
            var firstId = first.Occupancy.MinionId;
            var secondId = second.Occupancy.MinionId;
            var firstAt = first.Occupancy.Coordinate;
            var secondAt = second.Occupancy.Coordinate;
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, first.transform.position), Is.True,
                "The first miniature is named by clicking it where it stands.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "One name is not two: the selection is still open after the first click. " +
                Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, MinionView(secondId).transform.position),
                Is.True, "and the second is named the same way.");
            yield return null;
            yield return ConfirmIfStillStanding(root, pairNode);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Both names answer the card. " + Remaining(runtime));
            Assert.That(MinionView(firstId).Occupancy.Coordinate, Is.EqualTo(secondAt));
            Assert.That(MinionView(secondId).Occupancy.Coordinate, Is.EqualTo(firstAt),
                "The two miniatures exchanged the spaces they were standing on.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate,
                Is.EqualTo(stoodAt),
                "and Sabina did not move: this face names two minions and never herself.");
        }

        /// <summary>
        /// Troop Movement: "Move a friendly minion in radius 1 space, to a space in radius. May repeat
        /// once."
        ///
        /// <para>The face that asks a player for the most: which minion, where it goes, and whether to
        /// do it again. The two printed radii are measured from two different places - who may be
        /// ordered is bounded from Sabina, and where they may go is bounded from Sabina as well, which
        /// is what milestone 8.22's <c>WithinActorRadius</c> destination policy exists for and what
        /// keeps the destination set from being "anywhere one space away".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TroopMovement_MovesAFriendlyMinionByTwoClicksAndDeclinesItsRepeat()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.TroopMovement);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                "Troop Movement's printed Initiative 4 acts before the lowest card the enemy holds.");
            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Question one: which friendly minion is being ordered.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed Skill must stop and ask which minion moves. " + Remaining(runtime));
            var offered = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty,
                "A minion selection is answered on the map. " + Remaining(runtime));
            var orderNode = StandingNode(runtime);
            var ordered = offered[0];
            var orderedId = ordered.Occupancy.MinionId;
            var orderedFrom = ordered.Occupancy.Coordinate;
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, ordered.transform.position), Is.True,
                "The ordered minion is chosen by clicking it where it stands.");
            yield return null;
            yield return ConfirmIfStillStanding(root, orderNode);

            // Question two: where it goes. One space for the minion, and inside Sabina's own radius.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The order's destination must be asked once the minion is named. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "A space-valued question is answered on the board. " + Remaining(runtime));
            foreach (var destination in destinations)
            {
                Assert.That(HexDistanceQuery.Calculate(destination, orderedFrom), Is.EqualTo(1),
                    "\"1 space\" bounds how far the miniature travels, measured from the miniature.");
                Assert.That(HexDistanceQuery.Calculate(destination, sabinaAt),
                    Is.LessThanOrEqualTo(2),
                    "\"to a space in radius\" bounds where it may end, measured from Sabina - the " +
                    "second of the two printed radii and the one a lateral shuffle would break.");
            }

            var moveNode = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(Tile(chosen).IsHighlighted, Is.False);
            Assert.That(Tile(chosen).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, moveNode);

            Assert.That(MinionView(orderedId).Occupancy.Coordinate, Is.EqualTo(chosen),
                "The ordered miniature is standing on the space that was clicked.");

            // Question three: the printed repeat, refused on its own control.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"May repeat once\" must be asked once the order has been carried out. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates, Is.Empty,
                "and it is a decision rather than a selection. " + Remaining(runtime));
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "A printed \"may\" must be refusable by name. " + Remaining(runtime));
            var commandsBefore = runtime.AcceptedCommands.Count;
            UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsBefore + 1),
                "One click on the repeat's control is one accepted command.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Declining the repeat ends the card's text after its one order. " +
                Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate,
                Is.EqualTo(sabinaAt),
                "and the hero who gave the order never moved.");

            yield return CaptureGameView("troop-movement-orders-the-friendly-minion");
        }

        /// <summary>
        /// The same repeat decision, taken back and given again: an Undo across the answered decision,
        /// then a snapshot restored onto it, then the replay both of those rest on.
        ///
        /// <para>It is the printed repeat rather than either of the selections before it because a
        /// repeat is the one pause of hers a player is asked twice inside one card, so a re-derivation
        /// that came back with the wrong instance has something to be wrong about. The Undo half is
        /// proved on the live session before the capture, because a restore adopts a state rather than
        /// a history and clears the retained prefix behind it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TroopMovementsRepeat_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;

            yield return OrderAMinionUpToItsRepeat(root, sabinaId);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The journey needs the repeat standing. " + Remaining(runtime));
            var node = runtime.PendingEffectChoice.NodeId;
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // Answered, then taken back. The answer is an ordinary recorded command, so the retained
            // prefix replays every command before it and stops on the same question.
            UiPlayer.Click(UiPlayer.Button(hud, DeclineControl(runtime).Label));
            yield return null;
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause + 1),
                "One click on the repeat's control is one accepted command.");
            Assert.That(runtime.PendingEffectChoice, Is.Null);

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answer it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the repeat is standing again, replayed rather than remembered. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node),
                "about the same printed decision.");
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "and offering the refusal it offered before, which is derived from the node's own " +
                "floor rather than stored beside it.");

            // A snapshot taken on the stopped decision and restored onto the same session.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);
            UiPlayer.Click(UiPlayer.Button(hud, DeclineControl(runtime).Label));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The declined repeat is gone and the card is finished.");

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the same decision it was captured on. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));

            // and it is answerable on the same visible surface, by clicking the same real control.
            var restored = DeclineControl(runtime);
            Assert.That(restored, Is.Not.Null,
                "and it offers what the live pause offered, because nothing about the options is " +
                "persisted. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, restored.Label));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The click a player made after the restore answered the restored decision.");
        }

        /// <summary>
        /// p.10's secondary Defense icon, offered inside somebody else's Attack and spent on its own
        /// numeric face. Every one of Sabina's eighteen dealt cards prints one and not one of them
        /// prints a Defense primary, so this is one shape on eighteen faces rather than eighteen
        /// shapes - and the prompt she is shown has no whole-face primary in it at all, which is a
        /// thing about her set that no other hero's journeys can say.
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndSabinaSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);

            yield return TakeTheEnemysAttack(root, sabinaId, enemyId, SabinaCardContent.BackToBack);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime));
            var offers = runtime.PendingDefenseChoice.LegalDefenseOffers;
            Assert.That(offers, Is.Not.Empty,
                "Sabina always has something to discard: every dealt card of hers prints a secondary " +
                "Defense value.");
            Assert.That(
                offers.All(offer => OriginOf(runtime, offer) == ActionDefinitionOrigin.Reaction),
                Is.True,
                "and every one of them is the secondary icon, because she prints no Defense primary " +
                "anywhere in her set.");

            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "No offered face answers this Attack Total, so the prompt would prove the Defeat arm " +
                "instead. Shown: " + UiPlayer.Describe(hud));
            var faceName = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(survives.DefenseBreakdown.CardInstanceId.Value))
                .DefinitionReference.DefinitionId);
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(sabinaId) && card.State.Location == CardLocation.Hand);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. Shown: " + UiPlayer.Describe(hud));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(sabinaId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore - 1),
                "A Defense is paid by discarding the card from hand.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(sabinaId)), Is.True,
                "and the printed value answered the Attack, so Sabina is still standing.");
        }

        /// <summary>
        /// p.13's Defeat and respawn, with the piece as its subject: the token leaves the board when
        /// the answer is a decline, and comes back on a clicked spawn space at her own next turn. It
        /// is also the half of the spawn claim the engaged staging leaves open - the space she returns
        /// to is the map's own Blue hero spawn, not the space the staging moved her to.
        /// </summary>
        [UnityTest]
        public IEnumerator Sabina_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return TakeTheEnemysAttack(root, sabinaId, enemyId, SabinaCardContent.Quickdraw);
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndSabinaSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(sabinaId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(sabinaId)), Is.False,
                "and her token is gone with her rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            // p.13 offers the respawn at the defeated hero's own next turn, so the card she commits
            // for it has to take that turn: Point Blank Shot's printed 12 is above every card the
            // enemy still holds, and a lower one would leave the offer standing behind theirs.
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.PointBlankShot);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Sabina must be offered her respawn at her own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(sabinaId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Sabina token stands after the respawn, not a second one beside a stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True,
                "and it is her own official token that came back.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Turn one: Sabina spends the named card's secondary Movement to stand beside the enemy hero,
        /// which is the only board her printed Range 1 basic can shoot one from. One turn is one
        /// action here, so this cannot be folded into the turn the Attack is on; the walk itself is
        /// somebody else's journey and rides the headless surface.
        /// </summary>
        private static IEnumerator StepIntoContact(
            MatchSceneCompositionRoot root, HeroId sabinaId, HeroId enemyId, CardDefinitionId walkOn)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, sabinaId, walkOn);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                walkOn.Value + " must act before the lowest card the enemy holds.");

            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1 &&
                    HexDistanceQuery.Calculate(value.Destination, enemyAt) == 1)
                .ToArray();
            Assert.That(step, Is.Not.Empty,
                "No one-space step of hers stands beside the enemy hero, so this staging cannot " +
                "reach the board the printed Range 1 needs.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(step[0].Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, "The enemy holds the rest of turn one.");
            }

            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Turn one puts Sabina beside the enemy hero; turn two is the enemy's own adjacent Attack on
        /// her, stopped at p.14's Defense response. Which card she commits for turn two decides what
        /// is left in her hand to answer with, which is what tells the two Defense journeys apart.
        /// </summary>
        private static IEnumerator TakeTheEnemysAttack(
            MatchSceneCompositionRoot root, HeroId sabinaId, HeroId enemyId, CardDefinitionId sheCommits)
        {
            var runtime = root.Runtime;
            yield return StepIntoContact(root, sabinaId, enemyId, SabinaCardContent.TroopMovement);

            Commit(runtime, enemyId, WaspCardContent.Shock);
            Commit(runtime, sabinaId, sheCommits);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Shock's 8 takes the turn: a cross-team tie goes to the Tie Breaker side, which " +
                "`DEV-005` fixes to Red.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(sabinaId),
                "The enemy she walked up to must have her as a legal target.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(sabinaId)), Is.True);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Drives Troop Movement through its minion and its destination and stops on the printed
        /// repeat, which is the pause the restore/Undo journey is about. Every click here is a real
        /// board click - what the second journey adds is what happens to the answer afterwards.
        /// </summary>
        private static IEnumerator OrderAMinionUpToItsRepeat(
            MatchSceneCompositionRoot root, HeroId sabinaId)
        {
            var runtime = root.Runtime;
            var hud = root.Hud;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.TroopMovement);
            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            var offered = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty,
                "The order must offer a friendly minion to move. " + Remaining(runtime));
            var orderNode = StandingNode(runtime);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, offered[0].transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, orderNode);

            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "The order must offer somewhere to move it to. " + Remaining(runtime));
            var moveNode = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, moveNode);
        }

        /// <summary>
        /// Answers p.14's Defense with the face the prompt itself says leaves the defender standing.
        /// The outcome is read off the breakdown the player is shown rather than off a printed number,
        /// so a change to minion contributions or to an Ultimate's standing rules moves the journey
        /// with the game instead of stranding it on an arithmetic assumption.
        /// </summary>
        private static IEnumerator DefendSuccessfully(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "No offered face answers this Attack Total, so the defender would be defeated and " +
                "the rider under test would have no surviving target. " + Remaining(runtime));
            Assert.That(runtime.RoundPresenter.Submit(survives), Is.True);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// The offered Defense whose own preview says the defender survives it, or null when none does.
        /// </summary>
        internal static TypedInvocationViewModel SurvivingDefense(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.DefenseChoices.FirstOrDefault(choice =>
                choice.DefenseBreakdown != null &&
                choice.DefenseBreakdown.CardInstanceId.HasValue &&
                choice.DefenseBreakdown.Outcome == AttackResolutionOutcome.SuccessfullyDefended);

        /// <summary>The standing pause's own decline control, or null when it offers none.</summary>
        internal static TypedInvocationViewModel DeclineControl(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);

        internal static HeroId OtherHeroId(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => !hero.HeroId.Equals(heroId)).HeroId;

        /// <summary>
        /// Commits the named card out of the named seat's hand and the lowest-Initiative card out of
        /// every other seat's, headlessly. The tray-clicked commitment has its own journey in
        /// <see cref="SabinaUiJourneyPlayModeTests"/>, and rulebook p.7 resolves the higher printed
        /// Initiative first, so the other seat yielding is what makes the turn deterministic.
        /// </summary>
        internal static void CommitSoTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId actingHeroId, CardDefinitionId definitionId)
        {
            // Milestone 8.35g. The commitment phase this drives does not open while a finished turn
            // is still held for its own player, so the release goes above every read below.
            UiPlayer.EndAnyTurnInProgress(runtime);

            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
            {
                if (seat.Equals(actingHeroId))
                {
                    Commit(runtime, seat, definitionId);
                    continue;
                }

                var lowest = runtime.Cards
                    .Where(card => card.OwnerId.Equals(seat) &&
                        card.State.Location == CardLocation.Hand)
                    .OrderBy(card => runtime.Definitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .ThenBy(card => card.Id.Value, System.StringComparer.Ordinal)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(lowest.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }
        }

        internal static void Commit(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definitionId)
        {
            // Milestone 8.35g. A finished turn is held for its own player, so the commitment phase
            // this reads into does not open until that player hands it on. The release goes above
            // the read, exactly where the player presses it.
            UiPlayer.EndAnyTurnInProgress(runtime);

            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from {heroId.Value}'s hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>The selected actor's printed primary action, for a setup step that is not the subject.</summary>
        internal static ActionDefinitionId PrimaryActionOf(ConfiguredMatchSceneRuntime runtime) =>
            runtime.AvailableTurnActions.Single(candidate =>
                candidate.IsAvailable && OriginOf(runtime, candidate) ==
                    ActionDefinitionOrigin.PrintedPrimary).ActionDefinitionId;

        /// <summary>
        /// Ends whoever is acting until the actor is the named seat. A cross-team Initiative tie goes
        /// to the Tie Breaker side, which `DEV-005` fixes to Red, so a card of hers that ties the
        /// enemy's lowest still reaches its own turn - after theirs.
        /// </summary>
        internal static IEnumerator YieldUntilTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            // Milestone 8.35g. A standing release is not "nobody is acting" - it is the previous
            // player still holding their finished turn with the selection already emptied - so it
            // is pressed like any other turn ending rather than mistaken for the exit. The budget
            // doubles because every turn now costs two presses of the same control.
            for (var budget = 0; budget < 8; budget++)
            {
                if (runtime.PendingTurnRelease == null &&
                    (!runtime.SelectedActor.IsDefined ||
                        runtime.SelectedActor.ActorId.Equals(heroId)))
                {
                    yield break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The enemy's turn is setup for this journey and must be endable. " +
                    Remaining(runtime));
                yield return null;
            }

            Assert.Fail("The named seat never took a turn. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the confirm belonging to the node that was standing before the click, when one is
        /// still there. A selection of exactly one is confirmed by the presenter behind the click and
        /// a wider one is not, so the two are told apart by asking rather than by assuming.
        ///
        /// <para><b>The node has to be named, and that is what milestone 8.22's first Play Mode run
        /// found.</b> An unqualified "press whatever confirm is standing" pressed the <em>next</em>
        /// node's control: Troop Movement's printed repeat is a decision with a confirm and a decline
        /// on it, so confirming blindly after the destination click accepted the repeat instead of
        /// leaving it to be answered, and the journey met the second order's target question where it
        /// expected the printed choice.</para>
        /// </summary>
        internal static IEnumerator ConfirmIfStillStanding(
            MatchSceneCompositionRoot root, EffectNodeId node)
        {
            var runtime = root.Runtime;
            var standing = runtime.PendingEffectChoice;
            if (standing == null || !standing.NodeId.Equals(node))
            {
                yield break;
            }

            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm == null)
            {
                yield break;
            }

            UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
            yield return null;
        }

        /// <summary>The node a pause is standing on, for a confirm that must not reach past it.</summary>
        internal static EffectNodeId StandingNode(ConfiguredMatchSceneRuntime runtime)
        {
            var standing = runtime.PendingEffectChoice;
            Assert.That(standing, Is.Not.Null,
                "No effect pause is standing to read a node from. " + Remaining(runtime));
            return standing.NodeId;
        }

        /// <summary>Drains whatever end-of-round pauses stand until the next turn can be committed.</summary>
        internal static IEnumerator AdvanceToNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            // Milestone 8.35g. Every turn now costs TWO presses of the same control - the Hold or
            // stop that finishes the resolution, then the release that hands the turn on - so a
            // journey step that pressed once per turn arrives here with the round still resolving
            // and the commitment phase it wants never opening. This drives the rest of the round
            // the way a player does, pressing whatever END TURN currently means, and stops the
            // moment that control is no longer offered, so a decision this filler cannot answer is
            // left standing for the journey rather than swallowed by it.
            for (var budget = 0; budget < 12; budget++)
            {
                if (runtime.Phase != RoundResolutionPhase.ResolvingTurn ||
                    runtime.RoundView.Model.EndTurnInvocation == null)
                {
                    break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "A turn ending this filler was offered must be takeable.");
                yield return null;
            }

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

        internal static CardDefinitionId[] DiscardOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

        /// <summary>
        /// The spaces a paused effect is offering, in board order. Space-valued candidates come through
        /// <c>EffectChoiceDestinations</c> and object-valued ones through <c>EffectChoiceTargets</c>;
        /// reading the wrong one is how a board-answered question looks answerless.
        /// </summary>
        internal static HexCoordinate[] LitDestinations(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();

        /// <summary>The drawn miniatures standing on the given spaces, in the order the spaces come.</summary>
        internal static StandardMinionView[] MinionsAt(IEnumerable<HexCoordinate> spaces) =>
            spaces
                .Select(space => Object.FindObjectsByType<StandardMinionView>()
                    .FirstOrDefault(view => view.Occupancy != null &&
                        view.Occupancy.Coordinate.Equals(space)))
                .Where(view => view != null)
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
                "] defense=[" +
                string.Join(", ", model.DefenseChoices.Select(value => value.Label)) + "]";
        }

        /// <summary>Whether any hero or minion now stands on this space.</summary>
        internal static bool OccupantAt(ConfiguredMatchSceneRuntime runtime, HexCoordinate space) =>
            runtime.Heroes.Any(hero => hero.Coordinate.Equals(space)) ||
            Object.FindObjectsByType<StandardMinionView>()
                .Any(view => view.Occupancy != null && view.Occupancy.Coordinate.Equals(space));

        /// <summary>
        /// The first offered space whose own camera ray reaches the board rather than a hero piece
        /// standing over it. <c>PrimitiveHeroPointerInput</c> offers a ray to the hero selectors before
        /// the tiles, so a space drawn behind a piece is answered by clicking elsewhere on it.
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
        /// gate requires. A batch run skips it, so the same journeys serve both tiers.
        /// </summary>
        internal static IEnumerator CaptureGameView(string name)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = System.IO.Path.GetFullPath($"tmp/8-22-{name}.png");
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
    }
}
