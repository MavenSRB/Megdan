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
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.DodgerUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Dodger at rulebook p.15's later stages, where most of her kit becomes reachable at all.
    /// Milestone 8.35z34.
    ///
    /// <para><b>Why these journeys are here rather than on the base arm, and it is the milestone's own
    /// measurement.</b> Nine of her nineteen faces read empty minion Spawn points in the Battle Zone,
    /// and every such point is OCCUPIED at the engaged staging - so on the untouched board her Defense
    /// values are their plain printed numbers, Death Trap admits nobody, and p.19 hands nothing back.
    /// Tide of Darkness is the switch: while she performs any action, every space counts as being in
    /// the Battle Zone and as carrying a friendly minion Spawn point. The Tier III arms are therefore
    /// the only compositions in which several of her faces do anything, which is content rather than a
    /// staging convenience.</para>
    ///
    /// <para><b>The Tier III alternate arm deals the ENEMY a chosen hand, and that is content too.</b>
    /// Enfeeblement's reading is about what the enemy cannot do, and a restriction is only honest if
    /// the player who is stopped can see they are stopped. No card in Wasp's Red set at any Tier
    /// prints a repeat, so that arm seats her on Red and deals her the set that does.</para>
    /// </summary>
    public sealed class DodgerProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Aegis of Doom: printed Defense <c>4+</c>, "+4 if there are 2 or more empty spawn points in
        /// radius in the battlezone".
        ///
        /// <para>p.14 prints the <c>+</c> on the number itself and names what it means, so the whole
        /// sentence is a value declaration and there is no step: what a player does is answer an Attack
        /// with the face. What this journey adds beyond the offer is the clause - with her Ultimate up,
        /// every empty space in her printed Radius counts, so the raise is real and measurable rather
        /// than a number the staging happens not to reach.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AegisOfDoom_IsOfferedAsAWholeFaceAndItsPrintedPlusIsRaisedByHerUltimate()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);

            Assert.That(UltimateIsFaceup(runtime, dodgerId), Is.True,
                "This arm unlocks Tide of Darkness, which is what makes the clause reachable.");

            // Setup, headlessly, and it takes TWO turns rather than one - which milestone 8.35z34's
            // first Play Mode run is what established. Spending the secondary Movement ENDS the turn,
            // so a walk and an Attack cannot share one: the enemy steps beside her, the turn closes,
            // and the Attack opens on the next one. That is the same two-turn shape every earlier
            // hero's contact journey uses, and skipping it leaves no primary action to submit.
            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(runtime, enemyId, BesideHerAndTheMinion);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.Electroblast);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(pending.LegalTargetHeroIds, Has.Member(dodgerId),
                "The enemy stepped beside her, so she is a legal target.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    pending.ResolutionId, pending.ChoiceId, dodgerId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "p.14 asks the defender before it compares anything. " + Remaining(runtime));

            var aegisFace = NameOf(runtime, DodgerCardContent.AegisOfDoom);
            var aegis = runtime.RoundView.Model.DefenseChoices.FirstOrDefault(offer =>
                offer.Label.Contains(aegisFace));
            Assert.That(aegis, Is.Not.Null,
                "Aegis of Doom is in her Tier III hand and prints a PRIMARY Defense, so p.14 offers " +
                "the whole face in the defender's prompt. " + Remaining(runtime));

            // A defense face is a card element rather than a labelled button.
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", aegisFace));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", aegisFace));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(dodgerId)), Is.True,
                "Her printed 4 raised by the Ultimate's own board reading answers this Attack, so " +
                "she is still standing.");
            Assert.That(DiscardOf(runtime, dodgerId),
                Has.Member(DodgerCardContent.AegisOfDoom),
                "and the face she answered with is in her discard, which is p.14's own cost.");
        }

        /// <summary>
        /// Death Trap: "An enemy hero in radius who is adjacent to an empty spawn point in the battle
        /// zone discards a card, if able."
        ///
        /// <para>Her Silver basic, and the one face of hers whose sentence has two bounds measured from
        /// two different places: the disk is measured from Dodger and the adjacency from each
        /// candidate. With her Ultimate up every empty space carries a Spawn point, so the enemy hero
        /// standing on the open board satisfies the narrowing - and what a player then answers is which
        /// card leaves their hand, on the card's own face.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DeathTrap_NamesAnEnemyBesideAnEmptySpawnPointAndTakesACardFromTheirHand()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(UltimateIsFaceup(runtime, dodgerId), Is.True);

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.DeathTrap);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(dodgerId), Remaining(runtime));

            var enemyHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var selection = runtime.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null,
                "Death Trap names a hero, so it opens a selection. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The enemy hero is named by clicking the piece where they stand - and they qualify " +
                "because the Ultimate makes every empty space beside them a friendly Spawn point.");
            yield return null;
            yield return null;

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null,
                "\"discards a card\" is answered by the hero who discards, on their own hand. " +
                Remaining(runtime));

            var card = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(card, Is.Not.Null,
                "The discard is answered on a card's own face, which is milestone 8.15a's contract. " +
                Remaining(runtime));

            // Milestone 8.15a's contract draws a hand card as a card ELEMENT, so the click is on the
            // face rather than on a button carrying its label.
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", card.Label));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", card.Label));
            yield return null;
            yield return null;

            Assert.That(
                runtime.Cards.Count(value =>
                    value.OwnerId.Equals(enemyId) && value.State.Location == CardLocation.Hand),
                Is.EqualTo(enemyHandBefore - 1),
                "One card left the enemy's hand, which is the whole of what the sentence does.");
        }

        /// <summary>
        /// Burning Skull's step-7 rider: "After the attack: Move up to 1 minion adjacent to you 1 space
        /// to a space not adjacent to you."
        ///
        /// <para>A printed "up to", so the shove is declinable on its own control; a board click names
        /// the minion and a second names where it goes. What bounds the destination is where SHE
        /// stands rather than how far the minion may travel, which is the narrowing this journey drives
        /// rather than asserts.</para>
        ///
        /// <para><b>THE BOARD IS RE-STAGED SO THE SECOND CLICK IS A REAL CHOICE</b>, which is milestone
        /// 8.86's own correction of its first attempt. The shove is a direct Move of one space, and
        /// from a space beside her up to THREE outer-ring landings are legal - but the untouched
        /// staging left exactly one of them free, so milestone 8.46's rule that a choice of exactly one
        /// resolves in a single press closed the whole rider on the click that named the minion. The
        /// first re-statement read that as the destination being derived rather than asked, which was
        /// the wrong reading of a right measurement: the shape is real, the board was the limitation.
        /// <see cref="LoadStageWithRoomToShove"/> clears what blocks the other landings, the journey
        /// asserts the offer really holds two or more, and the space it clicks is one a player chose
        /// between - which is what the gate's <c>BoardSpaceSelection</c> row is about.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BurningSkull_ShovesTheMinionItJustHitAndIsAnsweredOnItsOwnConfirm()
        {
            yield return LoadStageWithRoomToShove(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.BurningSkull);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "Burning Skull is a plain Ranged Attack, so it opens p.14's target question " +
                "directly. " + Remaining(runtime));
            Assert.That(pending.LegalTargetMinionIds, Is.Not.Empty,
                "An enemy minion stands within her printed Range on the staged board.");

            // MILESTONE 8.86. The target is chosen to leave her own ring intact wherever the board
            // allows it: p.14's step 6 removes what this Ranged Attack hits, and the rider below is
            // about "1 minion ADJACENT TO YOU" rather than about the minion the attack took - which
            // is what her gate's own Edit Mode contract says in as many words
            // ("TheTwoSkullRiders_ShoveAnyAdjacentMinionOutOfHerRing").
            var her = runtime.Heroes.Single(hero => hero.HeroId.Equals(dodgerId)).Coordinate;
            var shot = pending.LegalTargetMinionIds
                .Select(MinionView)
                .OrderByDescending(view =>
                    HexDistanceQuery.Calculate(view.Occupancy.Coordinate, her))
                .First();
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, shot.transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"After the attack\" is p.14's step 7 and it asks. " + Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "A printed \"up to\" has to be declinable on its own control.");

            // MILESTONE 8.86, AND THIS JOURNEY IS ONE OF THE TWO THE AUDIT WAS ASKED FOR. Until now
            // it pressed whichever of the rider's answers happened to stand and finished on
            // `PendingEffectChoice is null`: it never named a minion, never clicked a space, and
            // would have read exactly the same on a board where the rider shoved nobody - while its
            // OWN gate row binds `BoardUnitSelection` and `BoardSpaceSelection` to it. That is
            // milestone 8.38f's lesson word for word: the gate cannot verify that a journey
            // exercises the shape it binds, and only reading the journey can.
            var shovable = Object.FindObjectsByType<StandardMinionView>()
                .Where(view => view.Occupancy != null)
                .Where(view => runtime.RoundView.Model.EffectChoiceTargets
                    .Any(target => target.Space.Equals(view.Occupancy.Coordinate)))
                .ToArray();
            Assert.That(shovable, Is.Not.Empty,
                "\"Move up to 1 minion adjacent to you 1 space to a space not adjacent to you\" - " +
                "the rider has to offer a minion standing in her own ring, or there is no shove for " +
                "a player to make and no board shape for the gate's two rows to be about. " +
                Remaining(runtime));
            // WHICH offered minion the journey names is measured rather than taken first: the
            // printed destination is "1 space to a space not adjacent to you", so the minion this
            // journey names is one whose own ring really holds TWO of them. One would close the
            // step on the naming click under milestone 8.46's exactly-one rule and leave the second
            // board click - the one the gate's `BoardSpaceSelection` row is about - undriven.
            var shoved = shovable.FirstOrDefault(view => view.Occupancy.Coordinate
                .GetNeighbors()
                .Count(space =>
                    IsFreeSpace(runtime, space) &&
                    HexDistanceQuery.Calculate(space, her) > 1) >= 2);
            Assert.That(shoved, Is.Not.Null,
                "At least one offered minion has to have TWO free spaces beside it that are out of " +
                "her own ring, which is what LoadStageWithRoomToShove clears the board for. " +
                "Offered: " + string.Join(", ", shovable.Select(view =>
                    view.Occupancy.MinionId.Value + "@" + view.Occupancy.Coordinate + " landings=" +
                    view.Occupancy.Coordinate.GetNeighbors().Count(space =>
                        IsFreeSpace(runtime, space) &&
                        HexDistanceQuery.Calculate(space, her) > 1))) + ". " +
                Remaining(runtime));
            var shovedId = shoved.Occupancy.MinionId;
            var shovedFrom = shoved.Occupancy.Coordinate;
            Assert.That(HexDistanceQuery.Calculate(shovedFrom, her), Is.EqualTo(1),
                "and every offered subject is one the printed \"adjacent to you\" admits. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, shoved.transform.position), Is.True,
                "The shoved minion is named by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            // THE SECOND BOARD CLICK, AND IT IS A CHOICE. The naming click leaves the destination
            // question standing because the re-staged board gives it more than one answer;
            // milestone 8.46's exactly-one rule is what would close it on a board that gave it one,
            // and asserting the COUNT here is what keeps this journey honest if a staging change
            // ever narrows the offer back to a single landing.
            var landings = LitDestinations(runtime);
            Assert.That(landings.Length, Is.GreaterThanOrEqualTo(2),
                "\"1 space to a space not adjacent to you\" is answered on the board, and this " +
                "journey needs it to be a CHOICE rather than a foregone conclusion - otherwise the " +
                "naming click resolves the whole rider under milestone 8.46's exactly-one rule and " +
                "nobody ever presses the space. Offer was: " +
                string.Join(", ", landings.Select(space => space.ToString())) + ". " +
                Remaining(runtime));
            foreach (var landing in landings)
            {
                Assert.That(HexDistanceQuery.Calculate(landing, shovedFrom), Is.EqualTo(1),
                    "\"1 space\" is an exact distance. Offer was: " +
                    string.Join(", ", landings.Select(space => space.ToString())) + ". " +
                    Remaining(runtime));
                Assert.That(HexDistanceQuery.Calculate(landing, her), Is.GreaterThan(1),
                    "and what bounds the destination is where SHE stands - \"to a space not " +
                    "adjacent to you\". Offer was: " +
                    string.Join(", ", landings.Select(space => space.ToString())) + ". " +
                    Remaining(runtime));
            }

            var chosenLanding = FirstClickableSpace(
                pointerInput,
                landings.Where(space => !space.Equals(shovedFrom)).ToArray());
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(chosenLanding).transform.position),
                Is.True,
                "and where it goes is the second board click. " + Remaining(runtime));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The rider is answered and the card is finished. " + Remaining(runtime));

            var shovedNow = MinionView(shovedId).Occupancy.Coordinate;
            Assert.That(shovedNow, Is.EqualTo(chosenLanding),
                "The minion stands on the space the player chose out of the offer, not on whichever " +
                "one the rules would have picked for them. " + Remaining(runtime));
            Assert.That(shovedNow, Is.Not.EqualTo(shovedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the shoved minion really left " +
                shovedFrom + ". " + Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(shovedNow, shovedFrom), Is.EqualTo(1),
                "\"1 space\" is the exact distance the rider prints. " + Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(shovedNow, her), Is.GreaterThan(1),
                "and it is out of her ring - \"to a space not adjacent to you\", which is the " +
                "whole of what the rider is for. " + Remaining(runtime));
        }

        /// <summary>
        /// MILESTONE 8.86, at the coordinator's correction. Loads the staged scene twice: once to
        /// READ where the composition put her and the miniatures, and again with every minion that
        /// stands on a space Burning Skull's rider could shove a neighbour onto removed.
        ///
        /// <para><b>Why the board has to be cleared at all.</b> The rider is a direct Move of one
        /// space "to a space not adjacent to you", so from a space beside her three of the subject's
        /// six neighbours are candidates. The untouched staging leaves exactly one of those free,
        /// and milestone 8.46's rule that a choice of exactly one resolves in a single press then
        /// closes the whole rider on the click that NAMED the minion - so the second board click the
        /// gate's <c>BoardSpaceSelection</c> row is about is never drawn, and a journey cannot drive
        /// what is not asked. The board is not a defect; a shove with one legal landing is printed
        /// behaviour. But a journey whose subject is a choice of landings has to be given a board
        /// that offers one, exactly as milestone 8.82's minion journey had to be given a minion.</para>
        ///
        /// <para>Nothing else about the staging moves. The heroes are placed by the scene's own
        /// composition on both passes, and only minions standing on the outer ring are removed - a
        /// minion beside her is never one of them, because the spaces cleared are all more than one
        /// space from her.</para>
        /// </summary>
        private static IEnumerator LoadStageWithRoomToShove(SceneCardContent stage)
        {
            yield return LoadStagedScene(stage);
            var runtime = Root().Runtime;
            var her = DodgerSeat(runtime).Coordinate;
            var landings = runtime.Setup.Minions
                .Where(minion => HexDistanceQuery.Calculate(minion.Coordinate, her) == 1)
                .SelectMany(minion => minion.Coordinate.GetNeighbors())
                .Where(space => HexDistanceQuery.Calculate(space, her) > 1)
                .ToArray();
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                removedMinions: runtime.Setup.Minions
                    .Where(minion => landings.Contains(minion.Coordinate))
                    .Select(minion => minion.MinionId)
                    .ToArray());
            yield return LoadStagedScene(stage);
        }

        /// <summary>
        /// Middlefinger of Death: "Choose one or both (on different targets) — • Target a unit adjacent
        /// to you. • Target a hero in range who has one or more cards in the discard."
        /// Milestone 8.35z45, and her last face to become playable at all.
        ///
        /// <para><b>What a player meets here that they meet nowhere else in her set.</b> Her three
        /// other modal Attacks print "Choose one", so answering the modal takes exactly one bullet and
        /// closes it. This one prints "one <em>or both</em>": the first click does NOT close the modal,
        /// the second bullet is still offered afterwards, and a Confirm control is what ends the
        /// question. That is p.10's multi arm drawn as controls, and it is the visible half of Ruling
        /// 5.</para>
        ///
        /// <para><b>The target question arrives as p.14's OWN, and that is the other visible
        /// difference.</b> Milestone 8.35z34's first Play Mode run established that her "Choose one"
        /// faces leave <c>PendingAttackTargetChoice</c> null - they bind their target through card text
        /// with <c>set-attack-target@1</c>, so what a player answers is the selection the chosen bullet
        /// opened. A choose-both Attack cannot work that way, because each bullet runs a COMPLETE p.14
        /// against the board as that sequence finds it. So the chosen bullets publish a plan and the
        /// ordinary attack-target prompt opens, once per sequence, under an identity ending
        /// <c>:seq-N</c>. A journey that asserted the effect-selection shape here would be asserting
        /// the wrong card.</para>
        ///
        /// <para><b>What this journey shows since milestone 8.70, and what it deliberately does
        /// NOT.</b> The second bullet's pile clause admits nobody on this board - nothing has been
        /// discarded when the modal is answered - so from that milestone it is GREYED at the choice
        /// rather than offered, and the journey's subject is the CONTROLS that reach a player: one
        /// live bullet, one greyed one drawn with its label and no command, the printed ceiling of two
        /// unmoved, a modal that stays open until the player ends it, and p.14's own target prompt
        /// behind it. What it does not try to show is the two clauses contrasted on one board at one
        /// instant, or a both-bullet answer: both are measured offline against a real match in
        /// <c>MiddlefingerOfDeathTests</c> and <c>ModalBulletGreyingTests</c>, the first of which
        /// stands a board with a discard in range precisely so "one, OR BOTH" is answerable at all.</para>
        ///
        /// <para><b>NOT YET EXECUTED.</b> Written and compile-checked at milestone
        /// 8.35z45; the Unity launcher was held for the whole session, so its first run is
        /// owed at integration alongside the rest of the tier.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MiddlefingerOfDeath_GreysTheBulletWithNoSubjectAndOpensACompletePageFourteenForTheOther()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.MiddlefingerOfDeath);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "p.10's modal asks which bullets before p.14 asks anything. " + Remaining(runtime));
            // The same query `TypeControls` below makes for the Respawn's type choice: the Select-kind
            // controls a paused choice is currently offering, as opposed to its Confirm.
            // MILESTONE 8.70. Exactly one bullet is a live control here, and the other is drawn
            // GREYED - "Target a hero in range who has one or more cards in the discard" names nobody
            // on this staging, where nothing has been discarded when the modal is answered. Until this
            // milestone both were live controls and taking the empty one spent the action applying
            // nothing; the user settled BR-Q19h the other way, so the choice reads the board.
            var bullets = TypeControls(runtime);
            Assert.That(bullets, Has.Length.EqualTo(1),
                "Only the bullet that names a unit is an answer a player may give. " +
                Remaining(runtime));
            Assert.That(runtime.RoundView.Model.UnavailableEffectChoices, Has.Count.EqualTo(1),
                "and the other keeps its label and its place with no command behind it, which is " +
                "what a greyed bullet IS on the rendered tier. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(2),
                "The printed ceiling is untouched: \"one, or both\" is what the card says, and the " +
                "grey is a reading of the board rather than a change to the card. " +
                Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
            yield return null;
            yield return null;

            // AND THE MODAL IS ENDED BY THE PLAYER, which is the "or both" half seen from the other
            // side. Below the ceiling, stopping is a real choice and the Confirm is the control that
            // offers it - and here there is nothing left to add, because the only other bullet is
            // greyed. Her three "Choose one" faces are already finished at this point; this one is
            // not, and that difference is what the face needed a contract of its own for.
            Assert.That(TypeControls(runtime), Is.Empty,
                "Nothing is left to select: the one live bullet is taken and the other is greyed. " +
                Remaining(runtime));
            var confirm = runtime.RoundView.Model.EffectChoices.SingleOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(confirm, Is.Not.Null,
                "A modal that may take more than one option has to be endable by the player while " +
                "an option remains untaken. " + Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "and confirming closes it. " + Remaining(runtime));

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "The first chosen bullet opens p.14's OWN target question - not the effect selection " +
                "her \"Choose one\" faces open - because each bullet runs a complete p.14 against the " +
                "board it finds. " + Remaining(runtime));
            Assert.That(pending.ResolutionId.Value, Does.EndWith(":seq-1"),
                "and it runs under an identity derived from the action's, because p.13's Defeat pays " +
                "out once per identity and two sequences may each defeat a unit.");
            // THE OFFERED LIST, NOT THE GUESSED ONE. This first asked `LegalTargetHeroIds` for the
            // enemy hero, and the first complete run of this journey returned it EMPTY: the bullet
            // prints "target a UNIT adjacent to you", and this staging holds the two heroes two spaces
            // apart by construction - the same fact milestone 8.35z15 recorded when three journeys
            // turned out to have only ever declined the prompt their names claimed to drive. The
            // adjacent unit here is a MINION, and hero and minion targets are DISJOINT lists on this
            // choice. So the assertion is now that the bullet admitted somebody at all, and the click
            // goes to whichever piece the rules named rather than to the one the journey assumed.
            Assert.That(pending.LegalTargets, Is.Not.Empty,
                "\"Target a unit adjacent to you\" - the staging has to put SOME unit beside her, or " +
                "this bullet's own p.14 has nothing to prove. " + Remaining(runtime));

            var named = pending.LegalTargets[0];
            var namedPosition = named.Kind == AttackTargetKind.Hero
                ? HeroView(named.HeroId).transform.position
                : Object.FindObjectsByType<StandardMinionView>()
                    .Single(view => view.BoundMinionId.Equals(named.MinionId))
                    .transform.position;

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, namedPosition), Is.True,
                "The target is named by clicking the piece where they stand.");
            yield return null;
            yield return null;

            // THE DEFENCE WINDOW IS THE TARGET'S, NOT THE SEQUENCE'S, and the first complete run of
            // this journey is what settled it. This asserted a window per sequence unconditionally;
            // p.14 asks a DEFENDER, and the unit this bullet admitted on this staging is a MINION,
            // which holds no hand and answers nothing. The Edit Mode contract already reads it that
            // way - `MiddlefingerOfDeathTests.RunOneSequence` returns early when the choice is null -
            // so asserting a window here claimed more than the rules promise. It is declined where it
            // is opened, because what a defender spends changes the Attack Total and nothing this
            // case reads.
            if (runtime.PendingDefenseChoice != null)
            {
                Assert.That(named.Kind, Is.EqualTo(AttackTargetKind.Hero),
                    "only a hero holds a hand to answer p.14 with. " + Remaining(runtime));
                // Milestone 8.46c: the decline is the decision window's own corner answer now.
                UiPlayer.ClickDecisionCorner(runtime, hud);
                yield return null;
                yield return null;
            }
            else
            {
                Assert.That(named.Kind, Is.EqualTo(AttackTargetKind.Minion),
                    "a hero target owes a defence window, so a missing one has to mean the bullet " +
                    "named a minion. " + Remaining(runtime));
            }

            // MILESTONE 8.86: "its complete p.14 ran" is asserted rather than asserted ABOUT. The
            // target question closing says the page moved on; the seat-public log's Defeat row says
            // the attack reached the consequence the chosen bullet promises.
            // MEASURED: the unit this staging puts beside her is a MINION, so the consequence is
            // p.14's step 6. The branch is kept because the bullet's own sentence admits either.
            if (named.Kind == AttackTargetKind.Minion)
            {
                Assert.That(MinionsDefeated(runtime), Is.Not.Empty,
                    "The chosen bullet's own complete p.14 reached its step 6. Log: " +
                    string.Join(" | ", runtime.Projection.MatchLog
                        .Select(entry => entry.Kind + "/" + entry.Outcome)) + ". " +
                    Remaining(runtime));
                Assert.That(
                    runtime.Projection.Minions.Any(minion =>
                        minion.MinionId.Equals(named.MinionId)),
                    Is.False,
                    "and the unit the bullet named is off the board. " + Remaining(runtime));
            }
            else
            {
                Assert.That(runtime.PendingDefenseChoice, Is.Null,
                    "A hero target's own p.14 response was answered, which is this bullet's " +
                    "consequence when the unit beside her is a hero. " + Remaining(runtime));
            }
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "One bullet was chosen, its complete p.14 ran, and the action is finished rather " +
                "than standing open. MILESTONE 8.70 is what makes that the whole of it: the second " +
                "bullet admits only a hero in range holding a card in the discard, which this staging " +
                "does not put there, so it was greyed at the modal instead of being taken and spent " +
                "on nothing. The ceiling of two is still the card's, and this journey now shows what " +
                "a board that supports only one of the two answers looks like to a player. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Weakness: "This turn: enemy heroes in radius have -4 Attack."
        ///
        /// <para>Her Active effect, raised from its own Skill control and then READ where a player
        /// meets it. What it is is a signed contribution on the Attack Total rather than a change to a
        /// printed value - the user's RULING 13 - so what the reading shows is the effect standing,
        /// not a number on a card face.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Weakness_StandsWhereAPlayerReadsItAndDrainsTheAttackTotalInRadius()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.Weakness);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;
            yield return null;

            var weaknessCardId = runtime.Cards
                .Single(card => card.OwnerId.Equals(dodgerId) &&
                    card.DefinitionReference.DefinitionId.Equals(DodgerCardContent.Weakness))
                .Id;
            Assert.That(runtime.IsActiveEffectSource(weaknessCardId), Is.True,
                "The raised Active effect must be readable against the card that raised it - a " +
                "drain nobody can attribute is a number that appears from nowhere. " +
                Remaining(runtime));

            // MILESTONE 8.86: the effect is read WHERE A PLAYER READS IT as well as in the
            // authority. An effect that draws no control of its own has exactly one surface - the
            // played card's own slot on her tray, wearing the Active outline - and a journey that
            // only asks the runtime whether a source is live is not standing where the player is.
            yield return PointTrayAt(root, dodgerId);
            yield return null;
            var slot = UiPlayer.NamedElementWithText(
                hud, "gd-card-slot", NameOf(runtime, DodgerCardContent.Weakness));
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "The card's slot wears the Active-effect outline while the drain lives, which is " +
                "the only place a player can read that it is standing at all.");

            // RECORDED GAP (milestone 8.86, and it is a gap in the JOURNEY rather than in the card).
            // "This turn: enemy heroes in radius have -4 Attack" is a signed contribution on the
            // Attack Total (the user's RULING 13), and the moment it fires is an enemy attack this
            // arc never reaches: nobody attacks inside it, so the -4 is proved standing here and
            // proved arithmetically offline rather than read off a breakdown a player is looking at.
            // Enfeeblement's journey below is the one that carries an enemy attack under the same
            // family of lock, and its attack takes a MINION, which holds no hand and draws no
            // Defense breakdown to read a row on. Raised in this milestone's report.
        }

        /// <summary>
        /// Enfeeblement: "This turn: enemy heroes in radius have -6 Attack and cannot repeat actions."
        ///
        /// <para>The restriction half, read as the greyed control the enemy is actually shown -
        /// rulebook p.17's restrictions are only honest if the player who is stopped can see they are
        /// stopped. The user's RULING 14 scopes it to p.10's Repeat keyword alone, so what must be
        /// greyed is a repeat control and nothing else.</para>
        ///
        /// <para><b>This arm deals the enemy a hand that can repeat</b>, because no card in Wasp's Red
        /// set at any Tier does and a restriction with nothing to restrict proves nothing.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Enfeeblement_GreysTheRepeatControlOfAnEnemyWhoStartedInsideHerRadius()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);

            Assert.That(
                runtime.Cards
                    .Where(card => card.OwnerId.Equals(enemyId) &&
                        card.State.Location == CardLocation.Hand)
                    .Select(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)))
                    .Any(definition => definition.ActionDefinitions.Any(action =>
                        action.EffectRoot != null &&
                        action.EffectRoot.Nodes.Any(node =>
                            node.Kind == EffectNodeKind.RepeatAction))),
                Is.True,
                "This arm exists to give the restriction something to restrict: the enemy must hold " +
                "a card that prints p.10's Repeat, or a greyed control would prove nothing.");

            // Thunder Boomerang prints p.10's Repeat and Initiative 9; Enfeeblement prints 11, so she
            // acts first and the restriction is standing before the enemy's own activation - which is
            // the shape milestone 8.20a's Spell Break journey established for a "This turn" lock.
            Commit(runtime, dodgerId, DodgerCardContent.Enfeeblement);
            Commit(runtime, enemyId, WaspCardContent.ThunderBoomerang);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;
            yield return null;

            var enfeeblementCardId = runtime.Cards
                .Single(card => card.OwnerId.Equals(dodgerId) &&
                    card.DefinitionReference.DefinitionId.Equals(DodgerCardContent.Enfeeblement))
                .Id;
            Assert.That(runtime.IsActiveEffectSource(enfeeblementCardId), Is.True,
                "Enfeeblement raises an Active effect, which is the half Weakness shares. " +
                Remaining(runtime));

            // Her turn finishes and is released, so the enemy's activation happens INSIDE the turn the
            // restriction covers. Milestone 8.35g made the release explicit and separate from the
            // finish; this journey needs both presses for the same reason Arien's Spell Break does -
            // and it presses END TURN itself rather than yielding until nobody is acting, which
            // milestone 8.35z34's first Play Mode run showed carries the round past both turns and
            // out of the window the restriction lives in.
            if (runtime.SelectedActor.ActorId.Equals(dodgerId))
            {
                UiPlayer.Click(
                    UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }

            Assert.That(UiPlayer.ReleaseFinishedTurn(runtime), Is.True,
                "The turn has to be released for the enemy to act inside it. " + Remaining(runtime));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId), Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's own Attack is setup; what this journey is about is its step 7. " +
                Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            var target = pending.LegalTargetMinionIds.Cast<MinionId?>().FirstOrDefault();
            Assert.That(target.HasValue, Is.True,
                "The enemy's Attack needs a subject for its step 7 to be reached at all. " +
                Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    pending.ResolutionId, pending.ChoiceId, target.Value)),
                Is.True);
            yield return null;
            yield return null;

            // THE READING. p.14's step 7 is where Thunder Boomerang's "may repeat" would be offered,
            // and RULING 14 scopes Enfeeblement to exactly p.10's Repeat keyword - so the one control
            // that must not be pressable is that one, and every other option the enemy has is
            // untouched. A restriction the stopped player cannot see is what this shape exists to
            // catch, so the assertion is about what is SHOWN rather than about what resolved.
            // MILESTONE 8.86, and it is rule 3 applied to this exact shape: the claim below is that
            // a control is ABSENT, and an absence proves nothing unless the moment it would have
            // been drawn at was actually reached. An attack that never resolved would satisfy it
            // word for word. So the attack's own resolution is read first, on the seat-public log.
            Assert.That(MinionsDefeated(runtime), Is.Not.Empty,
                "The enemy's Attack has to have REACHED ITS CONSEQUENCE for p.14's step 7 to be " +
                "the moment this journey is reading. Log: " +
                string.Join(" | ", runtime.Projection.MatchLog
                    .Select(entry => entry.Kind + "/" + entry.Outcome)) + ". " + Remaining(runtime));
            Assert.That(
                runtime.Projection.Minions.Any(minion => minion.MinionId.Equals(target.Value)),
                Is.False,
                "and the minion it named is off the board, so the attack really was carried " +
                "through to its own step 6. " + Remaining(runtime));

            var repeatConfirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(repeatConfirm, Is.Null,
                "Enfeeblement forbids p.10's Repeat, so the enemy is not offered a takeable repeat " +
                "at step 7. Shown controls: " + UiPlayer.Describe(hud));
            Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null,
                "and the enemy is stopped rather than stuck - p.11's Hold is unaffected, which is " +
                "the half RULING 14 kept: replacement actions remain available.");
        }

        /// <summary>
        /// Necromastery: "Respawn a friendly minion in an empty friendly spawn point in radius in the
        /// battle zone", with Tide of Darkness up - which is RULING 48's whole subject.
        ///
        /// <para>Two questions on one node. The destination is any empty space in her printed Radius,
        /// because the Ultimate grants a friendly Spawn point on every space; and because a granted
        /// point names no type, the type comes from rulebook p.19's own eligibility count, with the
        /// choice handed to the actor when the page admits more than one.</para>
        ///
        /// <para><b>The setup is p.19's own precondition and cannot be skipped.</b> "A minion must
        /// first be removed, before it can be respawned" - so this journey has the enemy defeat two
        /// friendly minions of DIFFERENT types before Dodger acts. One removal makes one type eligible
        /// and asks nothing; two make two eligible, which is the state the second question exists
        /// for.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Necromastery_RespawnsIntoAGrantedSpaceAndAsksWhichMinionTypeComesBack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(UltimateIsFaceup(runtime, dodgerId), Is.True,
                "RULING 48's reading is about a point the Ultimate granted, so the Ultimate has to " +
                "be up for this journey to be about it at all.");

            yield return RemoveTwoFriendlyMinionTypes(runtime, dodgerId, enemyId);

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.Necromastery);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(dodgerId), Remaining(runtime));

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var destinations = runtime.PendingEffectChoice;
            Assert.That(destinations, Is.Not.Null,
                "The Respawn asks which space first. " + Remaining(runtime));

            var lit = LitDestinations(runtime);
            Assert.That(lit, Is.Not.Empty,
                "\"Respawn anywhere in reach\": with the Ultimate up every empty space in her " +
                "printed Radius is a destination. " + Remaining(runtime));

            var granted = FirstClickableSpace(pointerInput, lit);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(granted).transform.position),
                Is.True,
                "The destination is answered on the board, like every other Place in this build.");
            yield return null;
            yield return null;

            var typeChoice = runtime.PendingEffectChoice;
            Assert.That(typeChoice, Is.Not.Null,
                "RULING 48: a granted point names no type, and p.19 admits two here, so p.10's " +
                "default hands the actor the choice. " + Remaining(runtime));

            yield return UiPlayer.WaitForDecisionWindow(runtime, hud);
            var typeControls = TypeControls(runtime);
            Assert.That(typeControls, Has.Length.GreaterThanOrEqualTo(2),
                "The second question offers the eligible types as its own controls - miniatures in " +
                "the SUPPLY, which is why it is not a board click. " + Remaining(runtime));

            var standingBefore = Object.FindObjectsByType<StandardMinionView>().Length;
            UiPlayer.Click(UiPlayer.Button(hud, typeControls[0].Label));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The answered type finishes the step. " + Remaining(runtime));
            Assert.That(Object.FindObjectsByType<StandardMinionView>(),
                Has.Length.EqualTo(standingBefore + 1),
                "A miniature came back off the supply and onto the board.");
            Assert.That(MinionsAt(new[] { granted }), Is.Not.Empty,
                "and it is standing on the space she chose - a point the map never printed.");
        }

        /// <summary>
        /// The Respawn's type choice, answered again after a retained-prefix Undo and again after a
        /// snapshot restore. It is the pause worth choosing for her session-operations row: milestone
        /// 8.35i's corrective is about a capability choice paused INSIDE an enclosing action, and this
        /// is the only decision of hers asked in the middle of a step that has already begun
        /// committing.
        /// </summary>
        [UnityTest]
        public IEnumerator TheRespawnsTypeChoice_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return RemoveTwoFriendlyMinionTypes(runtime, dodgerId, enemyId);

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.Necromastery);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);
            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var granted = FirstClickableSpace(pointerInput, LitDestinations(runtime));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(granted).transform.position),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The journey needs the type question standing. " + Remaining(runtime));
            var node = runtime.PendingEffectChoice.NodeId;
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // THE UNDO IS TESTED FIRST AND THE RESTORE SECOND, and the order is forced rather than
            // chosen. A snapshot restore permanently disables history - `AcceptedCommands` comes back
            // empty and UNDO has nothing to drop - so an Undo attempted after a restore is testing
            // nothing. Milestone 8.35z34's Play Mode iteration measured that too, having tried the
            // other order first to isolate the two operations.
            yield return UiPlayer.WaitForDecisionWindow(runtime, hud);
            var chosen = TypeControls(runtime).First();
            UiPlayer.ClickDecisionControl(runtime, hud, chosen);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null);

            // ANSWERING THIS QUESTION COSTS MORE THAN ONE ACCEPTED COMMAND, which milestone 8.35z34's
            // Play Mode iteration measured rather than assumed: a bounded Select is accepted as a
            // selection and then a confirmation, where the repeat decisions earlier heroes' journeys
            // undo are a single press.
            Assert.That(runtime.AcceptedCommands.Count, Is.GreaterThan(commandsAtPause),
                "The answer has to have been accepted for an Undo to have anything to drop.");

            // AND THE TWO QUESTIONS SHARE A NODE, which is the finding worth carrying out of this
            // journey. RULING 48 puts the destination and the type on ONE capability node, so
            // `PendingEffectChoice.NodeId` is the same value for both and cannot tell them apart - an
            // Undo that stepped one command too far left the SPACE question standing and satisfied a
            // node-identity assertion anyway. What distinguishes them is what each OFFERS: the type
            // question draws Select controls, and the destination question is answered on the board
            // and draws none. So the journey undoes one command at a time and stops on that
            // difference rather than counting presses.
            yield return UiPlayer.WaitForControl(hud, "DIRECTOR");
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            for (var press = 0; press < 4 && runtime.AcceptedCommands.Count > commandsAtPause; press++)
            {
                UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answer it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and a decision of the same Respawn is standing again, replayed rather than " +
                "remembered. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node),
                "inside the same enclosing Respawn.");
            Assert.That(TypeControls(runtime), Is.Not.Empty,
                "and it is the TYPE question rather than the destination one - the two share a node, " +
                "so this is the assertion that tells them apart. " + Remaining(runtime));

            // THE RESTORE HALF, taken from the pause the Undo returned to. What it proves is the one
            // milestone 8.35z34 had to FIX rather than merely check: a restored pause re-enters a node
            // through Execute rather than through Resume, so before that fix a snapshot captured here
            // came back showing the DESTINATION question - the answer this node already had was asked
            // for a second time.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            UiPlayer.ClickDecisionControl(runtime, hud, TypeControls(runtime).First());
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The answered type finishes the step. " + Remaining(runtime));

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the decision it was captured on - milestone " +
                "8.35i's corrective holding for a choice paused inside an enclosing action. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));

            var restored = TypeControls(runtime).FirstOrDefault();
            Assert.That(restored, Is.Not.Null,
                "and it is the TYPE question rather than the destination one, which is what says the " +
                "destination answer survived the round trip. The two questions share a node, so " +
                "nothing but the offered controls can tell them apart. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, restored.Label));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The click a player made after the restore answered the restored decision.");
        }

        /// <summary>
        /// Tide of Darkness: p.16's unlocked Ultimate, drawn faceup where a player can see it - and the
        /// only thing her Ultimate's own row claims, because the card starts no step and asks nobody
        /// anything. What it DOES is make the board read differently, which this journey measures
        /// through a face that reads it.
        /// </summary>
        [UnityTest]
        public IEnumerator TideOfDarkness_IsDrawnFaceupOnHerRailAndTurnsHerCountIntoEmptySpaces()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var dodgerId = DodgerSeat(runtime).HeroId;

            Assert.That(UltimateIsFaceup(runtime, dodgerId), Is.True,
                "p.16 puts the unlocked Ultimate faceup in the hero's area, which is a thing a " +
                "player must be able to see before it is a thing they can use.");

            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(dodgerId));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "Dodger's rail must draw her unlocked Ultimate. " + Remaining(runtime));
            Assert.That(rail.UltimateCard.DisplayName, Is.EqualTo("Tide of Darkness"),
                "and it is her own Ultimate rather than whatever the other seat unlocked.");
            Assert.That(rail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"),
                "drawn as unlocked, which is the state p.16 puts it in.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The controls the Respawn's TYPE question draws, which is what distinguishes it from the
        /// destination question standing on the same node.
        ///
        /// <para>RULING 48 puts both questions on one capability node, so their
        /// <c>PendingEffectChoice.NodeId</c> is the same value and a journey cannot tell them apart by
        /// identity. What differs is what each asks for: the destination is a space and is answered on
        /// the board, drawing no effect-choice control at all, while the type is a control naming a
        /// miniature in the supply. Every assertion in this fixture that means "the type question"
        /// says so through this rather than through the node.</para>
        /// </summary>
        // Milestone 8.46e: the type question is drawn on the decision window - its candidates are
        // miniatures in the SUPPLY, with no piece on the board to click - so its controls are read
        // off the window's model rather than the strip's, which no longer draws them.
        private static DecisionWindowControlViewModel[] TypeControls(
            ConfiguredMatchSceneRuntime runtime) =>
            (runtime.DecisionWindowView.Model?.Controls ??
                (IReadOnlyList<DecisionWindowControlViewModel>)System.Array.Empty<DecisionWindowControlViewModel>())
                .Where(control => control.Command?.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    control.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();

        /// <summary>
        /// MILESTONE 8.86. Every Defeat rulebook p.14's step 6 has written to the seat-public match
        /// log - the table-public record of an Attack having reached its consequence, and therefore
        /// the surface on which a journey can assert that an attack really ran without knowing
        /// either printed value or which minion defense modifiers the board contributed.
        ///
        /// <para><b>MEASURED at 8.86, and it is why this reads the Defeat rather than the Attack.</b>
        /// The log DOES carry a <c>MatchLogEntryKind.Attack</c> row for an attack on a minion, and
        /// that row's <c>Outcome</c> is <c>None</c>: <c>MatchLogOutcome</c>'s four members are about
        /// an attack a DEFENDER answered, and a minion holds no hand and answers nothing.</para>
        /// </summary>
        private static GuardsDigital.Core.Diagnostics.MatchLogEntry[] MinionsDefeated(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.Projection.MatchLog
                .Where(entry =>
                    entry.Kind ==
                        GuardsDigital.Core.Diagnostics.MatchLogEntryKind.MinionDefeated)
                .ToArray();

        /// <summary>
        /// Whether a space is playable under rulebook p.19 and carries no piece. Milestone 8.86:
        /// the shove's own destination has to be somewhere a minion can actually stand, so the
        /// journey measures that before it names the minion it shoves.
        /// </summary>
        private static bool IsFreeSpace(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate space) =>
            runtime.Projection.SelectedMap.Spaces.Any(value =>
                value.Coordinate.Equals(space) &&
                value.Kind == BoardMapSpaceKind.Playable) &&
            !runtime.Projection.Heroes.Any(hero => hero.Coordinate.Equals(space)) &&
            !runtime.Projection.Minions.Any(minion => minion.Coordinate.Equals(space)) &&
            !runtime.Projection.Obstacles.Any(obstacle => obstacle.Coordinate.Equals(space));

        private static bool UltimateIsFaceup(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards.Any(card =>
                card.OwnerId.Equals(heroId) &&
                card.State.Location == CardLocation.UltimateArea);

        /// <summary>
        /// p.19's own precondition, driven through the enemy's Attacks rather than staged by hand: two
        /// friendly minions of DIFFERENT types come off the board, so the page admits two types back
        /// and the Respawn's second question has something to ask.
        ///
        /// <para>It rides the headless command surface because it is setup - the same division every
        /// journey in this suite draws - and it asserts the state it produced rather than assuming it,
        /// so a staging change that made the removals impossible fails here with a reason instead of
        /// failing later as a missing control.</para>
        /// </summary>
        private static IEnumerator RemoveTwoFriendlyMinionTypes(
            ConfiguredMatchSceneRuntime runtime, HeroId dodgerId, HeroId enemyId)
        {
            var removedTypes = new List<StandardMinionType>();

            for (var attempt = 0; attempt < 4 && removedTypes.Count < 2; attempt++)
            {
                yield return AdvanceToNextTurn(runtime);

                // The card is chosen from what the enemy STILL HOLDS rather than named, because a
                // spent card cannot be committed again - which is what milestone 8.35z34's first Play
                // Mode run failed on, having named one card for every pass of this loop.
                var attack = AttackCardStillInHand(runtime, enemyId);
                if (attack == null)
                {
                    break;
                }

                CommitForRemovalRound(runtime, dodgerId, enemyId, attack.Value);
                yield return YieldUntilTheNamedSeatActs(runtime, enemyId);

                if (!runtime.SubmitAction(PrimaryActionOf(runtime)))
                {
                    continue;
                }

                yield return null;
                var pending = runtime.PendingAttackTargetChoice;
                if (pending == null)
                {
                    continue;
                }

                // A minion of a type not already removed, which is what makes the second removal a
                // second TYPE rather than a second miniature. Heavies are skipped because p.19's count
                // never favours one - RULING 12's derived exclusion - so removing one would buy this
                // journey nothing.
                var candidate = pending.LegalTargetMinionIds
                    .Select(MinionView)
                    .Where(view => view != null)
                    .Select(view => view.Occupancy)
                    .Where(minion => minion.MinionType != StandardMinionType.Heavy)
                    .FirstOrDefault(minion => !removedTypes.Contains(minion.MinionType));
                if (candidate == null)
                {
                    yield return YieldUntilNobodyIsActing(runtime);
                    continue;
                }

                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                        pending.ResolutionId, pending.ChoiceId, candidate.MinionId)),
                    Is.True);
                yield return null;
                yield return null;

                if (!Object.FindObjectsByType<StandardMinionView>()
                        .Any(view => view.Occupancy.MinionId.Equals(candidate.MinionId)))
                {
                    removedTypes.Add(candidate.MinionType);
                }

                yield return YieldUntilNobodyIsActing(runtime);
            }

            Assert.That(removedTypes, Has.Count.GreaterThanOrEqualTo(2),
                "p.19 admits a type back only once one of its miniatures has been removed, so this " +
                "journey needs two types removed before the Respawn has a second question to ask. " +
                "Removed: " + string.Join(", ", removedTypes) + ". " + Remaining(runtime));
        }

        /// <summary>
        /// One round of the removal setup, committing BOTH seats by hand rather than through the
        /// shared step - and the reason is the sharpest trap this milestone's Play Mode work hit.
        ///
        /// <para><c>CommitSoTheNamedSeatActs</c> commits the non-acting seat's LOWEST-Initiative card
        /// as a throwaway, which is exactly right until the card the journey still needs IS that card.
        /// Necromastery prints Initiative 2, the lowest in her Tier III hand, so every pass of the
        /// removal loop spent the very face the journey exists to play - and it failed later and
        /// elsewhere, as "no such card in hand" at the commitment that mattered rather than at the
        /// commitment that took it.</para>
        ///
        /// <para>The general shape, worth carrying: <b>a setup step that spends "whatever is cheapest"
        /// is unsafe for any hero whose subject card is their cheapest.</b> Dodger's Green line is her
        /// whole low-Initiative band, so she meets it where earlier heroes did not.</para>
        /// </summary>
        private static void CommitForRemovalRound(
            ConfiguredMatchSceneRuntime runtime,
            HeroId dodgerId,
            HeroId enemyId,
            CardDefinitionId enemyAttack)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);

            var throwaway = runtime.Cards
                .Where(card => card.OwnerId.Equals(dodgerId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => new
                {
                    Card = card,
                    Definition = runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference))
                })
                .Where(value => !value.Definition.Id.Equals(DodgerCardContent.Necromastery))
                .OrderBy(value => value.Definition.Initiative.Value)
                .FirstOrDefault();
            Assert.That(throwaway, Is.Not.Null,
                "This setup needs a card of hers to spend that is not the one it is preparing for.");

            Assert.That(runtime.SelectCommitmentCard(throwaway.Card.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);

            Commit(runtime, enemyId, enemyAttack);
        }

        /// <summary>
        /// An Attack-printing card the named seat still holds, or null when they hold none. Chosen
        /// from the hand rather than named, because a card spent on an earlier turn cannot be
        /// committed again.
        /// </summary>
        private static CardDefinitionId? AttackCardStillInHand(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            var candidates = runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary))
                .OrderByDescending(definition => definition.Initiative.Value)
                .ToArray();
            return candidates.Length == 0 ? (CardDefinitionId?)null : candidates[0].Id;
        }
    }
}
