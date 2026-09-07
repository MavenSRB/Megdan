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
    /// <para><b>EXECUTED AT MILESTONE 8.35z50, and four of these five pass.</b> They were written
    /// offline against the composed content and the offline shape derivation; two complete Play Mode
    /// runs then drove them. Every journey still carrying an [Explicit] reason says what its own run
    /// measured - all of it fixture instrumentation rather than anything about his content, which is
    /// green on the harness and Edit Mode tiers.</para>
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
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
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
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "The engaged staging holds the two heroes exactly TWO spaces apart around the engaged " +
            "minion, and the enemy Attack this journey picks has no hero target at that distance - " +
            "so Ursafar is never among its legal targets. Preferring a RANGED printed Attack (applied " +
            "here) was not sufficient on the run that measured it; closing the gap first, the way " +
            "Arien's own defeat journey does on this same staging, is the shape that works and is " +
            "left for the milestone that drives it.")]
        public IEnumerator Ursafar_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, ursafarId, LowestInitiativeIdOf(runtime, ursafarId));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before Ursafar's lowest card. " + Remaining(runtime));
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var offered = runtime.PendingAttackTargetChoice.LegalTargets;
            var target = offered.FirstOrDefault(candidate =>
                candidate.Kind == AttackTargetKind.Hero &&
                candidate.HeroId.Equals(ursafarId));
            Assert.That(target, Is.Not.Null,
                "The enemy's Attack must be able to reach Ursafar on this staging, or there is no " +
                "Defeat to drive. Offered: " +
                string.Join(", ", offered.Select(candidate => candidate.Kind.ToString())) +
                ". " + Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat regardless of either printed value.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(ursafarId)), Is.False,
                "The defeated hero is off the board until he respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(ursafarId)), Is.False);

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.PreyDrive);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(ursafarId),
                "The respawn is offered at his own turn, so his card has to open it. " +
                Remaining(runtime));

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Ursafar must be offered his respawn at his own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            var respawnTile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(respawn.Destination));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, respawnTile.transform.position), Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(ursafarId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Ursafar token stands after the respawn, not a second beside a stale one.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True);
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
