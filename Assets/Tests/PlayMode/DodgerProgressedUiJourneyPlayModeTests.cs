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
        /// </summary>
        [UnityTest]
        public IEnumerator BurningSkull_ShovesTheMinionItJustHitAndIsAnsweredOnItsOwnConfirm()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedDodger);
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

            var adjacent = pending.LegalTargetMinionIds
                .Select(MinionView)
                .First();
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, adjacent.transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"After the attack\" is p.14's step 7 and it asks. " + Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "A printed \"up to\" has to be declinable on its own control.");

            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm != null)
            {
                UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
                yield return null;
                yield return null;
            }

            // Whichever way the shove was opened, the journey answers it rather than leaving it
            // standing: a step 7 left unanswered would end the turn on an open question.
            if (runtime.PendingEffectChoice != null)
            {
                UiPlayer.Click(UiPlayer.Button(hud, DeclineControl(runtime).Label));
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The rider is answered and the card is finished. " + Remaining(runtime));
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
            var chosen = TypeControls(runtime).First();
            UiPlayer.Click(UiPlayer.Button(hud, chosen.Label));
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

            UiPlayer.Click(UiPlayer.Button(hud, TypeControls(runtime).First().Label));
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
        private static TypedInvocationViewModel[] TypeControls(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();

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
