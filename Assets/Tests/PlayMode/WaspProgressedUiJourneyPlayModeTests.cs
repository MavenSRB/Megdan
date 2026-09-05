using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.15: Wasp's Tier II and Tier III cards, and the unlocked Ultimate, played through
    /// the real player pipeline.
    ///
    /// <para>The scene composes the engaged verified mirror at the requested p.15 stage - the
    /// position the page's own procedure produces, which a live match cannot be seeded to - and each
    /// journey then resolves one upgraded card exactly as a player does: its controls are clicked as
    /// live elements, and every board decision goes through the camera's screen-point ray after the
    /// HUD's pointer-blocking answer. Commitment itself is proven through the real tray controls by
    /// the sibling journey fixtures, so these commit headlessly and spend their clicks on the card
    /// under test.</para>
    /// </summary>
    public sealed class WaspProgressedUiJourneyPlayModeTests
    {
        // ---------------------------------------------------------------------------------------
        // Tier II
        // ---------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Electrocute_PlayedAsItsOwnInstance_AsksItsTwoQuestionsThroughTheUi()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // Electrocute and Charged Boomerang both print Initiative 9: the cross-team tie selects
            // the Tie Breaker side's own card, which `DEV-005` fixes to Red, so the journey's actor
            // is deterministic.
            Commit(runtime, TeamSide.Red, WaspCardContent.Electrocute);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ChargedBoomerang);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var minionView = ClickableAdjacentMinionTarget(runtime);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, minionView.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Electrocute's own instance carries the same rider Shock's does, at its own icon.");
            var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, blueHeroView.transform.position),
                Is.True,
                "The enemy hero two spaces away is inside Electrocute's printed Radius 3 disk.");
            yield return null;

            yield return AnswerDiscardChooser(root, blueHeroId);
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(DiscardCount(runtime, blueHeroId), Is.EqualTo(1),
                "The chosen hero's clicked card left their hand.");
        }

        [UnityTest]
        public IEnumerator ChargedBoomerang_TargetsOffTheStraightLineAndDeflectBlocksItByItsFace()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // The defender's Block must stay in hand to be offered, so Red commits its Magnetic
            // Dagger, acts first at 12, and Holds as filler.
            Commit(runtime, TeamSide.Red, WaspCardContent.MagneticDagger);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ChargedBoomerang);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            // Milestone 8.35v: one press both Holds and releases.
            Assert.That(runtime.PendingTurnRelease, Is.Null);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetHeroIds, Does.Contain(redHeroId),
                "The bent two-space gap is in range and off every straight file.");
            var blueAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(blueHeroId)).Coordinate;
            Assert.That(
                choice.LegalTargetMinionIds.Select(id => MinionCoordinate(runtime, id)),
                Has.All.Matches<HexCoordinate>(coordinate =>
                    HexDistanceQuery.Calculate(coordinate, blueAt) > 1),
                "p.19: adjacency is always a straight line, so no adjacent unit is offered.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            yield return ClickDefenseFace(root, "DEFLECT PROJECTILES");

            // p.14: the Block's condition - a ranged attack - is fulfilled, so the defender stands
            // whatever the totals; and the rider excludes the attacker, who is the only enemy hero
            // in range on this board, so nobody is asked anything.
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Deflect's rider excludes the attacker and this board holds no other enemy hero.");
            Assert.That(
                runtime.Heroes.Any(hero => hero.HeroId.Equals(redHeroId)), Is.True,
                "The Block defends the hero whatever the attack total.");
            Assert.That(DiscardCount(runtime, redHeroId), Is.EqualTo(1),
                "p.14: the Block itself is paid by discarding the Block card.");
        }

        /// <summary>
        /// p.10's numeric secondary Defense icon, offered beside a Block and spent on its own face.
        /// Milestone 8.20b.
        ///
        /// <para>Fifteen of Wasp's nineteen faces print that icon and no journey had ever taken one:
        /// every Defense her fixtures answered was one of the three Green Blocks, which resolve the
        /// other way p.14 offers - outright, or with the defender defeated. This is the same prompt and
        /// the other transition, and it is the shape the gate's matrix rests those fifteen rows on.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsOfferedBesideTheBlockAndSpentOnItsOwnNumericFace()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // The same opening the Block journey uses: Red commits its Magnetic Dagger, acts first at
            // 12, and Holds, so the rest of its hand is still the hand p.14's step 3 reads.
            Commit(runtime, TeamSide.Red, WaspCardContent.MagneticDagger);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ChargedBoomerang);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            // Milestone 8.35v: one press both Holds and releases.
            Assert.That(runtime.PendingTurnRelease, Is.Null);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;

            // The subject: p.14's two Defense shapes in one prompt - the Block printed in the primary
            // slot, and the numeric secondary icons the rest of the hand prints.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            var defense = runtime.PendingDefenseChoice;
            Assert.That(
                defense.LegalDefenseOffers.Any(offer =>
                    OriginOf(runtime, offer) == ActionDefinitionOrigin.PrintedPrimary),
                Is.True,
                "The defender's Block must be one of the offers.");
            var numeric = defense.LegalDefenseOffers
                .Where(offer => OriginOf(runtime, offer) == ActionDefinitionOrigin.Reaction)
                .OrderByDescending(offer => PrintedDefenseOf(runtime, offer))
                .ThenBy(offer => DefinitionOf(runtime, offer).Id.Value, System.StringComparer.Ordinal)
                .ToArray();
            Assert.That(numeric, Is.Not.Empty,
                "and an ordinary secondary Defense icon must be another. Offers: " +
                string.Join(", ", defense.LegalDefenseOffers
                    .Select(offer => DefinitionOf(runtime, offer).DisplayName)));

            // The prompt as a player meets it: p.14's two Defense shapes drawn side by side as whole
            // printed faces, which is the half a batch tier cannot see.
            yield return UiPlayer.CaptureGameView("8-20b", "block-beside-numeric-defense-faces");

            var chosen = numeric[0];
            var printed = PrintedDefenseOf(runtime, chosen);
            Assert.That(printed, Is.GreaterThan(defense.AttackTotal),
                "This journey needs a printed Defense above the Attack Total, with headroom for the " +
                "minion contributions p.14 adds to both totals, so that what the numeric face does " +
                "is visible as the defender surviving. Printed " + printed + " against Attack Total " +
                defense.AttackTotal + ".");

            var faceName = DefinitionOf(runtime, chosen).DisplayName.ToUpperInvariant();
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(redHeroId) && card.State.Location == CardLocation.Hand);
            var acceptedBefore = runtime.AcceptedCommands.Count;
            yield return ClickDefenseFace(root, faceName);

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked numeric face answers p.14's response. Shown: " +
                UiPlayer.Describe(root.Hud));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore + 1),
                "One click on one card face is one accepted command.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(redHeroId)), Is.True,
                "p.14 compares the two totals rather than deciding outright, and here the comparison " +
                "leaves the defender standing - which is the outcome a Block reaches only by meeting " +
                "its own printed condition, and never by comparison at all.");
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(redHeroId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore - 1),
                "A Defense is paid by discarding the card from hand.");
            Assert.That(
                runtime.Cards.Single(card =>
                        card.OwnerId.Equals(redHeroId) &&
                        card.DefinitionReference.Equals(chosen.SourceDefinitionReference))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and it is the clicked card that was spent.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(redHeroId)).DiscardCards,
                Has.Count.EqualTo(1),
                "which the defender's own rail says where a player reads it.");
        }

        [UnityTest]
        public IEnumerator KineticRepulse_SelectsItsPushedUnitOnTheBoardAndConfirmsTheBatch()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            Commit(runtime, TeamSide.Red, WaspCardContent.KineticRepulse);
            Commit(runtime, TeamSide.Blue, WaspCardContent.Telekinesis);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Kinetic Repulse's 10 acts before Telekinesis' 3.");

            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The batch push asks which adjacent enemy units to take.");

            var engaged = ClickableEffectTargetMinion(runtime);
            var engagedId = engaged.Occupancy.MinionId;
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var distanceBefore = HexDistanceQuery.Calculate(
                MinionCoordinate(runtime, engagedId), redAt);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, engaged.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "A fixed distance of 3 opens no second question.");

            // The battle zone this board engages in can stop the push on its very first space -
            // p.18 resolves against the pre-push state and an occupied space is an obstacle, which
            // milestone 8.8's fixtures prove exactly. What a push can never do is bring the unit
            // closer, and what this journey owns is that the clicked selection and confirm ran the
            // batch to completion with no discard question for a pushed minion.
            Assert.That(
                HexDistanceQuery.Calculate(MinionCoordinate(runtime, engagedId), redAt),
                Is.GreaterThanOrEqualTo(distanceBefore),
                "A push moves its unit away from Wasp or not at all.");
        }

        [UnityTest]
        public IEnumerator ControlGravity_OrbitsAPieceAndItsRepeatIsDeclinedByItsControl()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            Commit(runtime, TeamSide.Red, WaspCardContent.DeflectProjectiles);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ControlGravity);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId),
                "Control Gravity's 10 acts before Deflect Projectiles' 3.");

            yield return StartActionThroughControl(root, ActionKind.Skill);
            var piece = ClickableEffectTargetMinion(runtime);
            var pieceId = piece.Occupancy.MinionId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, piece.transform.position), Is.True);
            yield return null;

            var destination = FirstLitDestination(runtime);
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, TileAt(destination).transform.position),
                Is.True);
            yield return null;
            Assert.That(MinionCoordinate(runtime, pieceId), Is.EqualTo(destination));

            // "May repeat once on the same target" - declined through its own control this time.
            yield return ClickEffectDecline(root);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Declining the repeat ends the Skill after its one move.");
            Assert.That(MinionCoordinate(runtime, pieceId), Is.EqualTo(destination),
                "and the piece stays where the single clicked move left it.");
        }

        [UnityTest]
        public IEnumerator Telekinesis_PlacesTheOffLineUnitBesideItsActorThroughBoardClicks()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // Telekinesis and Deflect Projectiles both print Initiative 3: the tie selects the Tie
            // Breaker side's card, so Red's Deflect acts first and Holds as filler.
            Commit(runtime, TeamSide.Red, WaspCardContent.DeflectProjectiles);
            Commit(runtime, TeamSide.Blue, WaspCardContent.Telekinesis);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its tied turn.");
            // Milestone 8.35v: one press both Holds and releases.
            Assert.That(runtime.PendingTurnRelease, Is.Null);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);

            // The Red hero at the bent two-space gap is in range and off the straight line, so the
            // enemy Wasp itself is a legal piece to place.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;

            var destination = FirstLitDestination(runtime);
            var blueAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(blueHeroId)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(destination, blueAt), Is.EqualTo(1),
                "Every offered destination is a space adjacent to the acting Wasp.");
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, TileAt(destination).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate,
                Is.EqualTo(destination),
                "The enemy Wasp stands on the clicked space beside the actor.");
        }

        // ---------------------------------------------------------------------------------------
        // Tier III
        // ---------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Electroblast_TakesACardFromTheChosenHeroWhoHoldsOneThroughTheUi()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // Electroblast and Thunder Boomerang both print Initiative 9: the tie selects Red.
            Commit(runtime, TeamSide.Red, WaspCardContent.Electroblast);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ThunderBoomerang);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var minionView = ClickableAdjacentMinionTarget(runtime);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, minionView.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, blueHeroView.transform.position),
                Is.True);
            yield return null;

            yield return AnswerDiscardChooser(root, blueHeroId);
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(DiscardCount(runtime, blueHeroId), Is.EqualTo(1),
                "Ledger question 13: a hero who can discard discards.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(blueHeroId)), Is.True,
                "and is not defeated, because the escalation is for a hero who cannot.");
        }

        [UnityTest]
        public IEnumerator ThunderBoomerang_IsReflectedThenItsRepeatIsDeclinedThroughTheControls()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // The Reflect must stay in the defender's hand, so Red commits its Magnetic Dagger,
            // acts first at 12, and Holds as filler.
            Commit(runtime, TeamSide.Red, WaspCardContent.MagneticDagger);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ThunderBoomerang);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            // Milestone 8.35v: one press both Holds and releases.
            Assert.That(runtime.PendingTurnRelease, Is.Null);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds,
                Does.Contain(redHeroId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            yield return ClickDefenseFace(root, "REFLECT PROJECTILES");

            // Reflect's rider: "an enemy hero in range discards a card, if able" - and unlike
            // Deflect's, it admits the attacker, who is the one enemy hero in range. The defender
            // clicks them on the board; the discard is then the attacker's own choice.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Reflect's rider admits the attacker and asks the defender to choose a hero.");
            var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, blueHeroView.transform.position),
                Is.True);
            yield return null;
            yield return AnswerDiscardChooser(root, blueHeroId);
            Assert.That(DiscardCount(runtime, blueHeroId), Is.EqualTo(1),
                "The reflected attacker paid exactly the clicked card.");
            Assert.That(DiscardCount(runtime, redHeroId), Is.EqualTo(1),
                "and the defender paid the Block itself, per p.14's discard.");

            // "If you targeted a hero may repeat once on a different target" - the attack targeted a
            // hero, blocked or not, so the repeat is offered and declined through its own control.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The repeat stands as its own question after the rider.");
            yield return ClickEffectDecline(root);
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "Declining the repeat starts no second Attack instance.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(redHeroId)), Is.True,
                "and the fulfilled Block kept the defender standing throughout.");
        }

        [UnityTest]
        public IEnumerator KineticBlast_AsksForItsDistanceAsRealControlsAfterItsUnitsAreClicked()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            // Kinetic Blast and Center of Mass both print Initiative 11: the tie selects Red.
            Commit(runtime, TeamSide.Red, WaspCardContent.KineticBlast);
            Commit(runtime, TeamSide.Blue, WaspCardContent.CenterOfMass);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));

            yield return StartActionThroughControl(root, ActionKind.Skill);
            var engaged = ClickableEffectTargetMinion(runtime);
            var engagedId = engaged.Occupancy.MinionId;
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var distanceBefore = HexDistanceQuery.Calculate(
                MinionCoordinate(runtime, engagedId), redAt);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, engaged.transform.position), Is.True);
            yield return null;

            // "3 or 4 spaces": after the units are named, the distance is one decision out of two,
            // offered as two real controls that read as the printed numbers themselves.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The written distance choice stops the Skill after its units are named.");
            var distances = runtime.RoundView.Model.EffectChoices
                .Where(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(distances.Select(value => value.Label), Is.EquivalentTo(new[] { "3", "4" }),
                "Exactly the printed 3 and 4 are offered, each reading as its number. Shown " +
                "controls: " + UiPlayer.Describe(root.Hud));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "4"));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(
                HexDistanceQuery.Calculate(MinionCoordinate(runtime, engagedId), redAt),
                Is.GreaterThanOrEqualTo(distanceBefore),
                "The clicked distance ran the push, which moves away from Wasp or not at all.");
        }

        [UnityTest]
        public IEnumerator CenterOfMass_RepeatsItsOrbitTwiceThroughItsOwnConfirmControls()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            Commit(runtime, TeamSide.Red, WaspCardContent.ReflectProjectiles);
            Commit(runtime, TeamSide.Blue, WaspCardContent.CenterOfMass);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId),
                "Center of Mass' 11 acts before Reflect Projectiles' 2.");

            yield return StartActionThroughControl(root, ActionKind.Skill);
            var piece = ClickableEffectTargetMinion(runtime);
            var pieceId = piece.Occupancy.MinionId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, piece.transform.position), Is.True);
            yield return null;

            // "May repeat up to two times on the same target": three moves in all, each destination
            // clicked on the board, each repeat accepted by its own control, and no third offer.
            for (var move = 0; move < 3; move++)
            {
                var destination = FirstLitDestination(runtime);
                Assert.That(
                    UiPlayer.ClickBoard(
                        pointerInput, root.Hud, TileAt(destination).transform.position),
                    Is.True,
                    $"Move {move + 1} must click a lit orbit space.");
                yield return null;
                Assert.That(MinionCoordinate(runtime, pieceId), Is.EqualTo(destination));
                if (move < 2)
                {
                    yield return ClickEffectConfirm(root);
                }
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Two accepted repeats exhaust the printed maximum, so no third question stands.");
        }

        [UnityTest]
        public IEnumerator MassTelekinesis_PlacesItsPieceAndItsUnboundRepeatIsDeclined()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            Commit(runtime, TeamSide.Red, WaspCardContent.Electroblast);
            Commit(runtime, TeamSide.Blue, WaspCardContent.MassTelekinesis);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Electroblast's 9 acts before Mass Telekinesis' 2.");
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds as filler.");
            // Milestone 8.35v: one press both Holds and releases.
            Assert.That(runtime.PendingTurnRelease, Is.Null);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            yield return StartActionThroughControl(root, ActionKind.Skill);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;
            var destination = FirstLitDestination(runtime);
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, TileAt(destination).transform.position),
                Is.True);
            yield return null;
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate,
                Is.EqualTo(destination));

            // "May repeat once", with no target binding: the second instance would ask for its own
            // piece. Declining it through its control ends the Skill after one Place.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            yield return ClickEffectDecline(root);
            Assert.That(runtime.PendingEffectChoice, Is.Null);
        }

        [UnityTest]
        public IEnumerator HighVoltage_IsVisibleOnTheRailAndFiresAfterTheBasicSkillThroughTheUi()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            // Milestone 8.15: the unlocked Ultimate is player-visible state - one marker per rail,
            // revealing the full printed face like every other rail icon.
            var ultimateMarkers = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-ultimate-card")
                .ToArray();
            Assert.That(ultimateMarkers, Has.Length.EqualTo(2),
                "Both mirrored Wasps unlocked High Voltage, and each rail says so.");
            var redRail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(redHeroId));
            Assert.That(redRail.UltimateCard, Is.Not.Null);
            Assert.That(redRail.UltimateCard.DisplayName, Is.EqualTo("High Voltage"));
            Assert.That(redRail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"));

            Commit(runtime, TeamSide.Red, WaspCardContent.StaticBarrier);
            Commit(runtime, TeamSide.Blue, WaspCardContent.MassTelekinesis);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Static Barrier's 13 acts before Mass Telekinesis' 2.");

            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // "Each time after you perform a basic skill, you may defeat an enemy minion in radius" -
            // first the may itself, a confirm-or-decline, and accepting it opens the minion
            // question, answered by clicking the minion where it stands.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "High Voltage must fire after the performed basic Skill.");
            yield return ClickEffectConfirm(root);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Accepting the may opens the minion question.");
            var engaged = ClickableEffectTargetMinion(runtime);
            var engagedId = engaged.Occupancy.MinionId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, engaged.transform.position), Is.True);
            yield return null;

            // "an enemy hero who was adjacent to that minion discards a card, if able" - the Blue
            // Wasp stood beside the defeated minion, and is clicked, then chooses its own card.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The was-adjacent hero question follows the defeat.");
            var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, blueHeroView.transform.position),
                Is.True);
            yield return null;
            yield return AnswerDiscardChooser(root, blueHeroId);

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(minionsBefore - 1),
                "The chosen minion is defeated and its view is gone.");
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(engagedId));
            Assert.That(DiscardCount(runtime, blueHeroId), Is.EqualTo(1),
                "and the was-adjacent Wasp paid a card.");
        }

        [UnityTest]
        public IEnumerator HighVoltage_DeclinedThroughItsControlDefeatsNobody()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            Commit(runtime, TeamSide.Red, WaspCardContent.StaticBarrier);
            Commit(runtime, TeamSide.Blue, WaspCardContent.MassTelekinesis);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));

            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);

            yield return ClickEffectDecline(root);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"you may defeat\" declined is a complete answer.");
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(minionsBefore),
                "No minion is defeated and no hero is asked for a card.");
            Assert.That(DiscardCount(runtime, blueHeroId), Is.EqualTo(0));
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        private static HeroId HeroOf(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;

        private static int DiscardCount(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards.Count(card =>
                card.OwnerId.Equals(heroId) &&
                card.State.Location == CardLocation.DiscardPile);

        private static HexCoordinate MinionCoordinate(
            ConfiguredMatchSceneRuntime runtime, MinionId minionId) =>
            runtime.BoardView.Model.Minions
                .Single(minion => minion.MinionId.Equals(minionId)).Coordinate;

        private static HexTileView TileAt(HexCoordinate coordinate) =>
            Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(coordinate));

        /// <summary>
        /// The first lit destination of the paused effect frame, in a stable order, read from the
        /// rendered model exactly as the lit board is drawn from it.
        /// </summary>
        private static HexCoordinate FirstLitDestination(ConfiguredMatchSceneRuntime runtime)
        {
            var destinations = runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty,
                "The paused frame offers at least one destination on this board.");
            var destination = destinations.First();
            Assert.That(TileAt(destination).IsHighlighted, Is.False);
            Assert.That(TileAt(destination).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement));
            return destination;
        }

        /// <summary>
        /// The minion view of one board-answerable target of the paused effect frame, in a stable
        /// order - the engaged minion always qualifies, and any further minion the position admits is
        /// as legal.
        /// </summary>
        private static StandardMinionView ClickableEffectTargetMinion(
            ConfiguredMatchSceneRuntime runtime)
        {
            var targets = runtime.RoundView.Model.EffectChoiceTargets
                .Select(target => target.Space)
                .ToArray();
            Assert.That(targets, Is.Not.Empty, "The paused frame names at least one board target.");
            var minion = runtime.BoardView.Model.Minions
                .Where(value => targets.Contains(value.Coordinate))
                .OrderBy(value => value.MinionId.Value, System.StringComparer.Ordinal)
                .First();
            return Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(minion.MinionId));
        }

        /// <summary>
        /// The minion view of one legal target of the pending Attack, in a stable order.
        /// </summary>
        private static StandardMinionView ClickableAdjacentMinionTarget(
            ConfiguredMatchSceneRuntime runtime)
        {
            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.LegalTargetMinionIds, Is.Not.Empty,
                "The engaged minion stands adjacent and is a legal target.");
            var minionId = choice.LegalTargetMinionIds
                .OrderBy(value => value.Value, System.StringComparer.Ordinal)
                .First();
            return Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(minionId));
        }

        /// <summary>Headless filler commitment; the tray's own controls are proven elsewhere.</summary>
        private static void Commit(
            ConfiguredMatchSceneRuntime runtime, TeamSide side, CardDefinitionId definitionId)
        {
            // Milestone 8.35g. Every actor now costs two presses of the same control, so the
            // commitment phase this reads into is not open until the turn in progress is finished.
            UiPlayer.EndAnyTurnInProgress(runtime);

            var heroId = runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from the {side} hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        private static IEnumerator StartActionThroughControl(
            MatchSceneCompositionRoot root, ActionKind kind)
        {
            // One frame first: the headless commitments above rebuilt the contextual region this
            // very frame, and an element has no resolved layout until the panel's next update.
            yield return null;
            var runtime = root.Runtime;
            var action = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == kind);
            UiPlayer.Click(UiPlayer.Button(root.Hud, action.Label));
            yield return null;
        }

        /// <summary>The printed definition one offered action belongs to.</summary>
        private static CardDefinition DefinitionOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            runtime.Definitions.Single(definition =>
                definition.Reference.Equals(offer.SourceDefinitionReference));

        /// <summary>Where on its card this offered action is printed. Milestone 8.20b.</summary>
        private static ActionDefinitionOrigin OriginOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            DefinitionOf(runtime, offer).ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .Origin;

        /// <summary>
        /// The Defense value this offer's face prints, or zero where it prints none - which is what a
        /// p.14 Block does, and why a Block cannot be picked by "the biggest number".
        /// </summary>
        private static int PrintedDefenseOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            DefinitionOf(runtime, offer).ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .BaseValues
                .Where(value => value.Channel == PrintedValueChannel.Defense)
                .Select(value => value.Amount)
                .DefaultIfEmpty(0)
                .First();

        /// <summary>Clicks one legal Defense card by its whole printed face.</summary>
        private static IEnumerator ClickDefenseFace(MatchSceneCompositionRoot root, string printedName)
        {
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(root.Hud, "gd-defense-card", printedName));
            yield return null;
            UiPlayer.Click(
                UiPlayer.NamedElementWithText(root.Hud, "gd-defense-card", printedName));
            yield return null;
        }

        /// <summary>
        /// Answers the chosen hero's own discard by clicking one offered card - a whole printed face
        /// since milestone 8.15a, found by the name on its own band exactly as a Defense face is.
        /// </summary>
        private static IEnumerator AnswerDiscardChooser(
            MatchSceneCompositionRoot root, HeroId chooserHeroId)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The discard is the chosen hero's own question.");
            var choices = runtime.RoundView.Model.EffectChoices
                .Where(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(choices, Is.Not.Empty,
                "The chooser's cards are offered. Shown controls: " + UiPlayer.Describe(root.Hud));
            Assert.That(choices.Select(value => value.Card), Has.All.Not.Null,
                "Each offer is the whole printed face rather than a text button.");
            var faceName = choices[0].Card.DisplayName.ToUpperInvariant();
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(
                UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
        }

        /// <summary>Clicks the paused frame's confirm control.</summary>
        private static IEnumerator ClickEffectConfirm(MatchSceneCompositionRoot root)
        {
            var confirm = root.Runtime.RoundView.Model.EffectChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(confirm, Is.Not.Null,
                "The frame offers its confirm. Shown controls: " + UiPlayer.Describe(root.Hud));
            UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
            yield return null;
        }

        /// <summary>Clicks the paused frame's decline control.</summary>
        private static IEnumerator ClickEffectDecline(MatchSceneCompositionRoot root)
        {
            var decline = root.Runtime.RoundView.Model.EffectChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            Assert.That(decline, Is.Not.Null,
                "The frame offers its decline. Shown controls: " + UiPlayer.Describe(root.Hud));
            UiPlayer.Click(UiPlayer.Button(root.Hud, decline.Label));
            yield return null;
        }

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

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static ActionKind ActionKindOf(
            ConfiguredMatchSceneRuntime runtime, ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }
    }
}
