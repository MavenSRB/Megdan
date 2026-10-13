using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Ursafar the Savage's piece, his tray and the Director drawer, driven through real pointer
    /// events and camera rays. Milestone 8.35z50, and the TENTH hero to take the
    /// player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals, and Ursafar is not among them - the
    /// roster-expansion amendment has not placed him in an official line-up. Every arm that deals him
    /// restages both heroes into the engagement milestone 8.15 built.</para>
    ///
    /// <para><b>He needs no ally arm.</b> Not one of his nineteen faces names a friendly hero at all -
    /// his whole set reads his own state (whether he is enraged) and the enemy or their minions.</para>
    ///
    /// <para><b>REPAIRED AND ACTIVE AT MILESTONE 8.36f3.</b> The original offline fixtures included
    /// stale staging assumptions; the audit replaced those assumptions with valid player-reachable
    /// setups and returned every journey to the ordinary Play Mode gate.</para>
    ///
    /// <para>The driving helpers are shared rather than copied, on milestone 8.35z34's precedent: what
    /// is Ursafar's own is <see cref="UrsafarSeat"/> and the journeys.</para>
    /// </summary>
    public sealed class UrsafarUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnUrsafarsOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Ursafar on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view.");

            var ursafarSetup = UrsafarSeat(runtime);
            var ursafarView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(ursafarSetup.HeroId));

            Assert.That(ursafarView.HeroContentId.HasValue, Is.True);
            Assert.That(ursafarView.HeroContentId.Value.Value,
                Is.EqualTo(UrsafarCardContent.ContentSetId));
            Assert.That(ursafarView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(ursafarView.HasTokenVisual, Is.True);
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(ursafarSetup.HeroId)).DisplayName,
                Is.EqualTo("Ursafar the Savage"));

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(ursafarSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(ursafarSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(ursafarSetup.HeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, ursafarView.transform.position), Is.True,
                "The camera ray at his piece must be claimed by the board rather than swallowed.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(ursafarSetup.HeroId));

            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(ursafarSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(UrsafarCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()),
                "Eighteen faces, because p.20's Jaws That Bite is in the box.");
        }

        [UnityTest]
        public IEnumerator Ursafar_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var ursafarId = UrsafarSeat(runtime).HeroId;

            // Claws That Catch prints Initiative 12 and Sec Mov 1, the highest of the two starting-hand
            // faces that carry a secondary Movement icon at all - Angry Roar prints none, exactly the
            // shape Misa's Swoop In and Dodger's Death Trap already have on the roster's other basics.
            yield return CommitEverySeat(root, ursafarId, MovingCardOf(runtime, ursafarId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(ursafarId));
            Assert.That(runtime.GetCommitmentStatus(ursafarId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(ursafarId)).Coordinate;
            var destination = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .First();
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(ursafarId)).Coordinate,
                Is.EqualTo(destination));
            // MILESTONE 8.86: and it is not the space he set off from, so the walk walked.
            Assert.That(destination, Is.Not.EqualTo(startedAt),
                "The one-cost destination the journey clicked is a space away from his own.");
            var ursafarView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(ursafarId));
            Assert.That(ursafarView.Coordinate, Is.EqualTo(destination),
                "with his token standing where he now is rather than where he was.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(ursafarId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesUrsafarsCommitmentAndRestartsHimOntoHisStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var ursafarSetup = UrsafarSeat(runtime);
            yield return PointTrayAt(root, ursafarSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, ursafarSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(ursafarSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(ursafarSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));

            yield return PointTrayAt(root, ursafarSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, ursafarSetup.HeroId));
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty);
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            var ursafarView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(ursafarSetup.HeroId));
            Assert.That(ursafarView.Coordinate, Is.EqualTo(ursafarSetup.Coordinate));
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(ursafarSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(UrsafarCardContent.StartingHand.Count));
        }

        /// <summary>
        /// Milestone 8.20c's standard-footprint conformance, on Ursafar's own printed set. His is a
        /// real test of the footprint rather than a formality: Claws That Catch and Tear both print
        /// text at TWO of p.14's moments on one card, and Evolutionary Response prints the roster's
        /// first "choose one, or both" modal.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryUrsafarFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Ursafar", UrsafarCardContent.AllDefinitions, root.Hud);
        }

        /// <summary>
        /// p.13's Defeat and respawn, on Ursafar's own piece - the same shape every dealt hero's own
        /// component row proves for itself. Declining the Defense outright is what makes the enemy's
        /// Attack a Defeat regardless of either side's printed values.
        /// </summary>
        [UnityTest]
        public IEnumerator Ursafar_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick()
        {
            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar, enemyDistance: 1);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            Commit(runtime, enemy, WaspCardContent.MagneticDagger);
            Commit(runtime, own, UrsafarCardContent.SniffOut);
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargets.Any(target =>
                target.Kind == AttackTargetKind.Hero && target.HeroId.Equals(own)), Is.True);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(own).transform.position), Is.True);
            yield return null; yield return null;
            yield return UrsafarAuditStaging.AnswerDefense(false);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(own)), Is.False);
            // A noninteractive defeat ghost may remain until the strike finishes.
            yield return new WaitForSeconds(3f);
            Assert.That(runtime.SpotlightView.Model.IsContinueArmed, Is.True);
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONTINUE"));
            var cameraDeadline = Time.realtimeSinceStartup + 10f;
            while (runtime.SpotlightView.IsDrivingCamera && Time.realtimeSinceStartup < cameraDeadline)
                yield return null;
            Assert.That(runtime.SpotlightView.IsDrivingCamera, Is.False);
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.PreyDrive);
            yield return null;
            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty, Remaining(runtime));
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            var tile = Object.FindObjectsByType<HexTileView>().Single(value => value.Coordinate.Equals(respawn.Destination));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, tile.transform.position), Is.True);
            yield return null;
            var views = Object.FindObjectsByType<PrimitiveHeroView>().Where(view => view.Occupancy.HeroId.Equals(own)).ToArray();
            Assert.That(views, Has.Length.EqualTo(1));
            Assert.That(views[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(views[0].HasTokenVisual, Is.True);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        internal static CardDefinitionId LowestInitiativeIdOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .OrderBy(definition => definition.Initiative.Value)
                .First()
                .Id;

        internal static CardDefinitionId HighestInitiativeAttackIdOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary))
                // MILESTONE 8.35z50's FIRST PLAY MODE RUN. Ordering by Initiative alone picked an
                // Attack whose printed reach does not cover the gap this staging actually holds:
                // `EngagedMirrorStaging` puts the two heroes exactly TWO spaces apart around the
                // engaged minion - its own doc comment calls that "a legal ranged-Attack line" - so a
                // non-ranged Attack has no hero target at all here and the journey died naming a
                // target that was never offered. A RANGED printed Attack is preferred first and the
                // Initiative order only breaks the tie, which is what makes this pick about the board
                // the journey is standing on rather than about a number.
                .OrderByDescending(definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.AttackRange == AttackRangeKind.Ranged))
                .ThenByDescending(definition => definition.Initiative.Value)
                .First()
                .Id;

        internal static QuickGameHeroSetupState UrsafarSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == UrsafarCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Ursafar to exactly one seat; it dealt him to " +
                seats.Length + ".");
            return seats[0];
        }
    }
}
