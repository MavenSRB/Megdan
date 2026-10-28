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
        [UnityTest]
        public IEnumerator ExperimentalOneLife_RemainsOneAfterAProgressedSceneGraphIsInstalled()
        {
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);
            var initial = Root().Runtime.InitialState;
            var stagedWithExperiment = new GuardsDigital.Application.MatchSessionInitialState(
                initial.Scenario,
                initial.Setup,
                initial.AvailableMaps,
                initial.InitialConfiguration,
                initial.InitialTiming,
                initial.StagedProgression,
                new GuardsDigital.Application.ExperimentalMatchOptions(
                    heroDefeatBaseLifeCostIsOne: true),
                initial.ModeRules);
            MatchSceneCompositionRoot.NextComposedExternalMatch =
                new ExternalMatchComposition(stagedWithExperiment);
            yield return LoadStage(SceneCardContent.VerifiedTierTwoEngagedMirror);

            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var redTeamId = runtime.ReadModel.Participants.GetTeamForHero(redHeroId).Id;
            var before = runtime.ReadModel.TeamLifeCounters
                .Single(entry => entry.Key.Equals(redTeamId)).Value;

            // The same real Tier-II opening as Charged Boomerang's journey below. Red holds, Blue
            // attacks the levelled hero and the defender declines, so p.13 must spend a Life.
            Commit(runtime, TeamSide.Red, WaspCardContent.MagneticDagger);
            Commit(runtime, TeamSide.Blue, WaspCardContent.ChargedBoomerang);
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its earlier turn.");
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            Assert.That(runtime.DeclineDefense(), Is.True);
            yield return null;
            yield return null;

            var after = runtime.ReadModel.TeamLifeCounters
                .Single(entry => entry.Key.Equals(redTeamId)).Value;
            Assert.That(after, Is.EqualTo(before - 1),
                "The gold-only experiment marker and the installed authority must agree: a " +
                "Tier-II hero Defeat spends one base Life, not two.");
        }

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
            var attackedId = minionView.Occupancy.MinionId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, minionView.transform.position), Is.True);
            yield return null;

            // MILESTONE 8.86. "Target a unit adjacent to you" is an ATTACK and its outcome is what
            // this journey never read: p.13 takes an attacked minion off the board, so the named
            // piece has to be gone before the rider below is worth anything.
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(attackedId),
                "\"Target a unit adjacent to you\" - the minion the Attack named is defeated.");

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
            yield return LoadStageWithRoomToPush(SceneCardContent.VerifiedTierTwoEngagedMirror);
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

            // MILESTONE 8.86. This used to take whichever unit came first and then assert the
            // distance had not DECREASED - which is true of a unit that never left its space, so the
            // journey could not tell a push from a stand-still. It names a unit whose first space
            // away from her is playable and empty, and reads the board afterwards.
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var engaged = PushableEffectTargetMinion(runtime, redAt,
                "Push up to 2 enemy units adjacent to you 3 spaces");
            var engagedId = engaged.Occupancy.MinionId;
            var pushedFrom = MinionCoordinate(runtime, engagedId);
            var distanceBefore = HexDistanceQuery.Calculate(pushedFrom, redAt);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, engaged.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "A fixed distance of 3 opens no second question.");

            // THE ASSERTION THIS JOURNEY WAS MISSING: the named unit really left the space it stood
            // on, and it went AWAY from her, which is the direction p.18 gives a push. The battle
            // zone can still stop it short of the printed three - "move it as far as possible" - so
            // the distance is asserted as strictly greater rather than as exactly three.
            Assert.That(MinionCoordinate(runtime, engagedId), Is.Not.EqualTo(pushedFrom),
                "\"Push up to 2 enemy units adjacent to you 3 spaces\" - the unit is standing where " +
                "it started, so nothing was pushed.");
            Assert.That(
                HexDistanceQuery.Calculate(MinionCoordinate(runtime, engagedId), redAt),
                Is.GreaterThan(distanceBefore),
                "and it is further from Wasp than it was, which is the only direction a push has.");
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

            // MILESTONE 8.86: the destination has to be one that TRAVELS. A lit space equal to the
            // piece's own is what an orbit offers when nothing else is reachable, and asserting the
            // piece stands on the space that was clicked is then true of a piece that never moved.
            var orbitedFrom = MinionCoordinate(runtime, pieceId);
            var destination = ATravellingDestination(runtime, orbitedFrom,
                "Move a unit, or a token, in radius 1 space, without moving it away from you or " +
                "closer to you");
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, TileAt(destination).transform.position),
                Is.True);
            yield return null;
            Assert.That(MinionCoordinate(runtime, pieceId), Is.EqualTo(destination));
            Assert.That(MinionCoordinate(runtime, pieceId), Is.Not.EqualTo(orbitedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the piece really left the space it stood on.");

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
            var placedFrom = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;

            var destination = ATravellingDestination(runtime, placedFrom,
                "Place a unit or a token in range, which is not in a straight line, into a space " +
                "adjacent to you");
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
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate,
                Is.Not.EqualTo(placedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the Place moved the enemy Wasp off the " +
                "space it was standing on rather than offering and doing nothing.");
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
            var attackedId = minionView.Occupancy.MinionId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, minionView.transform.position), Is.True);
            yield return null;

            // MILESTONE 8.86: the Attack's own printed outcome, read before its rider.
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(attackedId),
                "\"Target a unit adjacent to you\" - the minion the Attack named is defeated.");

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
            yield return LoadStageWithRoomToPush(SceneCardContent.VerifiedTierThreeEngagedMirror);
            var root = Root();
            var runtime = root.Runtime;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            // Kinetic Blast and Center of Mass both print Initiative 11: the tie selects Red.
            Commit(runtime, TeamSide.Red, WaspCardContent.KineticBlast);
            Commit(runtime, TeamSide.Blue, WaspCardContent.CenterOfMass);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));

            yield return StartActionThroughControl(root, ActionKind.Skill);
            // MILESTONE 8.86, exactly as on Kinetic Repulse: the unit is named because its first
            // space away from her is playable and empty, so the push this journey is about has
            // somewhere to go.
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var engaged = PushableEffectTargetMinion(runtime, redAt,
                "Push up to 2 enemy units adjacent to you 3 or 4 spaces");
            var engagedId = engaged.Occupancy.MinionId;
            var pushedFrom = MinionCoordinate(runtime, engagedId);
            var distanceBefore = HexDistanceQuery.Calculate(pushedFrom, redAt);
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
            Assert.That(MinionCoordinate(runtime, engagedId), Is.Not.EqualTo(pushedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the clicked distance ran a push that " +
                "really left the space it started on. It used to read GreaterThanOrEqualTo, which a " +
                "unit that never moved satisfies.");
            Assert.That(
                HexDistanceQuery.Calculate(MinionCoordinate(runtime, engagedId), redAt),
                Is.GreaterThan(distanceBefore),
                "and it is further from Wasp than it was, which is the only direction a push has.");
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
            //
            // MILESTONE 8.86: each of the three is asserted to have TRAVELLED. Reading the piece
            // back onto the space that was clicked is true of a piece standing still when the space
            // clicked was its own, so all three moves could have been nothing at all.
            for (var move = 0; move < 3; move++)
            {
                var stoodAt = MinionCoordinate(runtime, pieceId);
                var destination = ATravellingDestination(runtime, stoodAt,
                    "Move a unit, or a token, in radius 1 space, without moving it away from you " +
                    "or closer to you. May repeat up to two times on the same target");
                Assert.That(
                    UiPlayer.ClickBoard(
                        pointerInput, root.Hud, TileAt(destination).transform.position),
                    Is.True,
                    $"Move {move + 1} must click a lit orbit space.");
                yield return null;
                Assert.That(MinionCoordinate(runtime, pieceId), Is.EqualTo(destination));
                Assert.That(MinionCoordinate(runtime, pieceId), Is.Not.EqualTo(stoodAt),
                    $"Move {move + 1} really moved the piece rather than re-seating it.");
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
            var placedFrom = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, redHeroView.transform.position), Is.True);
            yield return null;
            var destination = ATravellingDestination(runtime, placedFrom,
                "Place a unit or a token in range, which is not in a straight line, into a space " +
                "adjacent to you. May repeat once");
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, TileAt(destination).transform.position),
                Is.True);
            yield return null;
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate,
                Is.Not.EqualTo(placedFrom),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the Place really moved the piece.");

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
            var ultimateMarkers = UiPlayer.ItemStripDescendants(hud)
                .Where(element => element.name == "gd-ultimate-card")
                .ToArray();
            Assert.That(ultimateMarkers, Has.Length.EqualTo(2),
                "Both mirrored Wasps unlocked High Voltage, and each rail says so.");
            var redRail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(redHeroId));
            Assert.That(redRail.UltimateCard, Is.Not.Null);
            Assert.That(redRail.UltimateCard.DisplayName, Is.EqualTo("High Voltage"));
            Assert.That(redRail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"));

            // Milestone 9.30a: the card now sits in the items strip, and a pointer rest on it there
            // still reveals the same full printed face the rest of the rail reveals.
            var redMarker = UiPlayer.Descendants(HeroRailCoinCheck.Rail(hud, TeamSide.Red))
                .Single(element => element.name == "gd-ultimate-card");
            UiPlayer.PointAt(redMarker);
            yield return null;
            Assert.That(runtime.HeroOverviewsView.HighlightedDiscardCard?.DisplayName,
                Is.EqualTo("High Voltage"),
                "A pointer rest on the strip's Ultimate reveals that card.");
            Assert.That(UiPlayer.IsShown(runtime.HeroOverviewsView.HoverPreviewRoot), Is.True,
                "and its whole face is drawn.");
            Assert.That(runtime.HeroOverviewsView.HoverPreviewRoot.childCount, Is.EqualTo(1));
            UiPlayer.PointAway(redMarker);
            yield return null;

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
        /// A lit destination of the paused effect frame that REALLY TRAVELS - read from the rendered
        /// model exactly as the lit board is drawn from it, in a stable order, with the space the
        /// piece is already standing on refused.
        ///
        /// <para><b>MILESTONE 8.86.</b> This used to return the first lit space whatever it was, and
        /// every caller then asserted the piece stood on the space it had clicked - which is true of a
        /// piece that never moved, because "the space it already occupies" is a legal answer to an
        /// "up to" move and is exactly what the board offers when nothing else is reachable. Milestone
        /// 8.82's class of vacuous pass in one line. The refusal is made here rather than per journey
        /// so no later caller can reintroduce it, and the message names the printed sentence the
        /// caller is proving.</para>
        /// </summary>
        private static HexCoordinate ATravellingDestination(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate from, string printed)
        {
            var destinations = runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty,
                "\"" + printed + "\" - the paused frame offers no destination at all on this board.");
            var travelling = destinations.Where(value => !value.Equals(from)).ToArray();
            Assert.That(travelling, Is.Not.Empty,
                "\"" + printed + "\" - every destination offered was the space the piece already " +
                "stood on, so the card could not move anything. Offered: " +
                string.Join(", ", destinations.Select(value => value.ToString())));
            var destination = travelling[0];
            Assert.That(TileAt(destination).IsHighlighted, Is.False);
            Assert.That(TileAt(destination).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement));
            return destination;
        }

        /// <summary>
        /// One board-answerable target of the paused push whose FIRST STEP AWAY from the actor is a
        /// playable, empty space - so the push it is about really travels rather than being stopped on
        /// the space it starts from.
        ///
        /// <para><b>MILESTONE 8.86.</b> Both push journeys used to take whichever unit came first and
        /// then assert the distance had not DECREASED, which a unit that never moved satisfies.
        /// p.18 pushes "directly away from the pushing unit", so the first space of that path is
        /// <c>2 * unit - actor</c> in axial terms, and a unit whose first space is off the map or
        /// occupied is a unit this journey may not choose.</para>
        /// </summary>
        private static StandardMinionView PushableEffectTargetMinion(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate actorAt, string printed)
        {
            var targets = runtime.RoundView.Model.EffectChoiceTargets
                .Select(target => target.Space)
                .ToArray();
            Assert.That(targets, Is.Not.Empty,
                "\"" + printed + "\" - the paused frame names no board target at all.");
            var occupied = runtime.BoardView.Model.Minions.Select(value => value.Coordinate)
                .Concat(runtime.Heroes.Select(value => value.Coordinate))
                .Concat(runtime.BoardView.Model.PlacedTokens.Select(value => value.Coordinate))
                .ToArray();
            var playable = Object.FindObjectsByType<HexTileView>()
                .Where(tile => tile.IsPlayable)
                .Select(tile => tile.Coordinate)
                .ToArray();
            var pushable = runtime.BoardView.Model.Minions
                .Where(value => targets.Contains(value.Coordinate))
                .Where(value => !Blocked(FirstSpaceAway(actorAt, value.Coordinate)))
                .OrderBy(value => value.MinionId.Value, System.StringComparer.Ordinal)
                .ToArray();
            Assert.That(pushable, Is.Not.Empty,
                "\"" + printed + "\" - every unit the push offered is already backed against an " +
                "obstacle, so no choice this journey could make would move anybody. Offered: " +
                string.Join("; ", targets.Select(space =>
                    space + " -> away " + FirstSpaceAway(actorAt, space) +
                    (playable.Contains(FirstSpaceAway(actorAt, space))
                        ? " playable" : " OFF THE MAP") +
                    (occupied.Contains(FirstSpaceAway(actorAt, space))
                        ? " OCCUPIED" : " empty"))));

            bool Blocked(HexCoordinate away) =>
                !playable.Contains(away) || occupied.Contains(away);
            return Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(pushable[0].MinionId));
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

            // MILESTONE 8.86: the chosen card is NAMED and its own transition is read afterwards.
            // This used to click `choices[0]` and leave the caller counting a pile, which cannot tell
            // the clicked card from any other card of anybody's reaching that pile.
            Assert.That(
                choices.Select(value =>
                    runtime.Cards.Single(card =>
                        card.Id.Equals(value.Command.EffectChoice.Candidate.Value.CardInstanceId))
                        .OwnerId),
                Has.All.EqualTo(chooserHeroId),
                "Every face offered belongs to the hero the text tells to discard.");
            var chosen = choices[0].Command.EffectChoice.Candidate.Value.CardInstanceId;
            var faceName = choices[0].Card.DisplayName.ToUpperInvariant();
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(
                UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and it is the clicked face that left the hand for the pile.");
        }

        /// <summary>
        /// Clicks the paused frame's confirm control - a printed `may`'s accept.
        ///
        /// <para>Milestone 8.46e: the accept is the decision window's control, labelled for what
        /// accepting does, so it is found by its command kind rather than by a label read off the
        /// strip. Every caller here presses a confirm-or-decline: Center of Mass and High Voltage
        /// reach it after a board-answered exactly-one selection that already resolved on its own
        /// click (milestone 8.46b), which is why the move is asserted BEFORE this is pressed and no
        /// DONE is ever expected for that selection.</para>
        /// </summary>
        private static IEnumerator ClickEffectConfirm(MatchSceneCompositionRoot root)
        {
            UiPlayer.ClickEffectControl(root.Runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
        }

        /// <summary>
        /// Clicks the paused frame's decline control. Milestone 8.46e: the decision window draws it
        /// beside the accept, named for what declining does, so it is found by command kind there.
        /// </summary>
        private static IEnumerator ClickEffectDecline(MatchSceneCompositionRoot root)
        {
            yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            UiPlayer.ClickEffectControl(root.Runtime, root.Hud, EffectChoiceCommandKind.Decline);
            yield return null;
        }

        /// <summary>
        /// p.18's first space of a push: the space one step further along the straight line from the
        /// pusher through the unit, which adjacency always makes a line (p.19). Axial arithmetic,
        /// because nothing on the board has an opinion about a direction.
        /// </summary>
        private static HexCoordinate FirstSpaceAway(HexCoordinate pusher, HexCoordinate unit) =>
            new HexCoordinate((unit.Q * 2) - pusher.Q, (unit.R * 2) - pusher.R);

        /// <summary>
        /// The Tier II/III engaged mirror, restaged so a push HAS SOMEWHERE TO GO. Milestone 8.86.
        ///
        /// <para><b>WHAT THE AUDIT FOUND, AND WHY THIS STEP EXISTS.</b> Both push journeys asserted
        /// that the unit's distance from Wasp had not DECREASED, and both passed for eleven
        /// milestones on a board where the one adjacent enemy unit is backed straight against
        /// another minion: p.18 resolves against the pre-push state, an occupied space is an
        /// obstacle, and "move it as far as possible" then moves it nought. So neither journey had
        /// ever pushed anybody, and neither could tell. The board is not a defect - a blocked push is
        /// printed behaviour - but a journey whose subject is a push has to be given a board the push
        /// can happen on, exactly as milestone 8.82's minion journey had to be given a minion.</para>
        ///
        /// <para>The scene is composed once to read where the mirror restaging put the pieces, and
        /// composed again with the minions standing in those first spaces removed. Nothing else
        /// about the staging moves: the heroes are placed by the same restaging on both passes,
        /// because the sandbox description is applied after it and does not touch them.</para>
        /// </summary>
        private static IEnumerator LoadStageWithRoomToPush(SceneCardContent stage)
        {
            yield return LoadStage(stage);
            var runtime = Root().Runtime;
            var redAt = runtime.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red).Coordinate;
            var behind = runtime.Setup.Minions
                .Where(minion => HexDistanceQuery.Calculate(minion.Coordinate, redAt) == 1)
                .Select(minion => FirstSpaceAway(redAt, minion.Coordinate))
                .ToArray();
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                removedMinions: runtime.Setup.Minions
                    .Where(minion => behind.Contains(minion.Coordinate))
                    .Select(minion => minion.MinionId)
                    .ToArray());
            yield return LoadStage(stage);
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
