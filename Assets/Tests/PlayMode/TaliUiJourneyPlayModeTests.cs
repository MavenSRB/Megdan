using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
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
    /// Tali the Spiritcaller's piece, her tray and the Director drawer, driven through real pointer
    /// events and camera rays. Milestone 8.37, and the twelfth hero to take the
    /// player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals, and Tali is not among them - the
    /// roster-expansion amendment has not placed her in an official line-up. Every arm that deals her
    /// restages both heroes into the engagement milestone 8.15 built.</para>
    ///
    /// <para><b>She needs no ally arm for reachability</b> - Commune with Spirits' mandatory naming
    /// already admits herself as a candidate friendly hero - but the ALLY-naming half of her full
    /// borrow flow needs one, which is what
    /// <see cref="TaliProgressedUiJourneyPlayModeTests"/>'s own staging supplies.</para>
    ///
    /// <para><b>EXECUTED AT MILESTONE 8.37, and fourteen of the sixteen were corrected by that run.</b>
    /// They were written offline against the composed content and the offline shape derivation, and
    /// the first Unity pass failed twelve - each for a reading the offline author could not have had.
    /// Two shipped parked and milestone 8.37a un-parked both, one of them by finding a shipped engine
    /// defect on the way. Nothing in this file is unrun any more.</para>
    ///
    /// <para>The driving helpers are shared rather than copied, on milestone 8.35z34's precedent: what
    /// is Tali's own is <see cref="TaliSeat"/> and the journeys.</para>
    /// </summary>
    public sealed class TaliUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnTalisOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Tali on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view.");

            var taliSetup = TaliSeat(runtime);
            var taliView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(taliSetup.HeroId));

            Assert.That(taliView.HeroContentId.HasValue, Is.True);
            Assert.That(taliView.HeroContentId.Value.Value,
                Is.EqualTo(TaliCardContent.ContentSetId));
            Assert.That(taliView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(taliView.HasTokenVisual, Is.True);
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(taliSetup.HeroId)).DisplayName,
                Is.EqualTo("Tali the Spiritcaller"));

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(taliSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(taliSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(taliSetup.HeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, taliView.transform.position), Is.True,
                "The camera ray at her piece must be claimed by the board rather than swallowed.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(taliSetup.HeroId));

            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(taliSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(TaliCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()),
                "Eighteen faces, because p.20's Ice Shard is in the box.");
        }

        [UnityTest]
        public IEnumerator Tali_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;

            // Winter Dagger prints Sec Mov 4, the highest of her starting-hand faces that carry a
            // secondary Movement icon at all - Commune with Spirits prints none, the one-icon-ribbon
            // shape her own inventory pass verified hardest.
            yield return CommitEverySeat(root, taliId, MovingCardOf(runtime, taliId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId));
            Assert.That(runtime.GetCommitmentStatus(taliId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(taliId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .First();
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(taliId)).Coordinate,
                Is.EqualTo(destination));
            // MILESTONE 8.86: the walk is a walk, and her token followed it.
            Assert.That(destination, Is.Not.EqualTo(startedAt),
                "Winter Dagger's printed boot really carried her off the space she started on.");
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>()
                    .Single(view => view.Occupancy.HeroId.Equals(taliId)).Coordinate,
                Is.EqualTo(destination),
                "and her token is drawn where she now is, not where she was.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(taliId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesTalisCommitmentAndRestartsHerOntoHerStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var taliSetup = TaliSeat(runtime);
            yield return PointTrayAt(root, taliSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, taliSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(taliSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(taliSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));

            yield return PointTrayAt(root, taliSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, taliSetup.HeroId));
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty);
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            var taliView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(taliSetup.HeroId));
            Assert.That(taliView.Coordinate, Is.EqualTo(taliSetup.Coordinate));
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(taliSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(TaliCardContent.StartingHand.Count));
        }

        /// <summary>
        /// Milestone 8.20c's standard-footprint conformance, on Tali's own printed set. Hers is a real
        /// test of the footprint rather than a formality: she is the first verified hero whose profile
        /// card's Tokens role icon is printed at all, and the five Green faces are the first component
        /// tables this fixture has to check against a token supply rather than an item track.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryTaliFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Tali", TaliCardContent.AllDefinitions, root.Hud);
        }

        /// <summary>
        /// p.13's Defeat and respawn, on Tali's own piece - the same shape every dealt hero's own
        /// component row proves for itself. Declining the Defense outright is what makes the enemy's
        /// Attack a Defeat regardless of either side's printed values.
        /// </summary>
        [UnityTest]
        public IEnumerator Tali_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // The engagement is two spaces wide, so the ATTACKER closes it first - the same whole-turn
            // step Tali's own adjacent faces take, taken by the other seat.
            yield return SpendATurnClosingTheGap(
                root, enemyId, taliId, TaliCardContent.GlacialBarrier);

            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            // Glacial Barrier opens her respawn two turns from here, so this filler may not spend it.
            Commit(runtime, taliId,
                LowestInitiativeIdOf(runtime, taliId, TaliCardContent.GlacialBarrier));
            // Which seat holds the turn first is p.7's Initiative comparison over the two cards this
            // round committed, and the gap-closing turn above changed what those cards are - so the
            // journey WAITS for the enemy's turn rather than asserting it arrives first.
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before Tali's lowest card. " + Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(taliId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat regardless of either printed value.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(taliId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(taliId)), Is.False);

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.GlacialBarrier);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Tali must be offered her respawn at her own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            var respawnTile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(respawn.Destination));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, respawnTile.transform.position), Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(taliId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Tali token stands after the respawn, not a second beside a stale one.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <param name="reserved">
        /// Faces a later turn of the same journey still needs, so this filler cannot spend them.
        /// </param>
        internal static CardDefinitionId LowestInitiativeIdOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId,
            params CardDefinitionId[] reserved) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .Where(definition => !reserved.Contains(definition.Id))
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
                .OrderByDescending(definition => definition.Initiative.Value)
                .First()
                .Id;

        /// <summary>
        /// Spends the committed card's SECONDARY MOVEMENT to step Tali next to the named enemy, and
        /// answers nothing else.
        ///
        /// <para><b>Why every adjacent-target journey of hers needs it.</b>
        /// <see cref="EngagedMirrorStaging"/> stands the two heroes exactly TWO spaces apart, each
        /// adjacent to one mid-board BLUE melee minion. Tali is the Blue seat, so that minion is
        /// friendly to her and the board she starts on holds no adjacent ENEMY unit at all - a face
        /// reading "target a unit adjacent to you" is offered nothing, and
        /// <c>LegalTargets.First(...)</c> throws rather than failing an assertion. Three of her
        /// journeys were written as though the engagement were adjacent; closing the gap first is
        /// what a player does, and rulebook p.14 lets the secondary Movement run before the primary.</para>
        ///
        /// <para><b>It spends a WHOLE TURN, which is not a choice.</b> A Movement with distance left
        /// over keeps asking, and the only control that finishes it early is END TURN - so closing a
        /// gap and then attacking cannot happen on one card. That is exactly why Arien's own journeys
        /// close their gap on turn one and attack on turn two, and this follows them: it commits a
        /// MOVING card, walks the mover adjacent, ends the turn and advances. The caller then commits
        /// the face under test for a turn that begins with the two of them already engaged.</para>
        /// </summary>
        /// <param name="reserved">
        /// Faces the caller needs in hand on a LATER turn, which this turn must therefore not spend on
        /// any seat. Without them the gap-closing turn happily commits the very card the journey
        /// exists to drive - it picks the highest-Initiative mover for the walker and the lowest card
        /// for everyone else, and Tali's Gold basic is both a mover and the subject of two of these
        /// journeys. A three-turn journey reserves every face it has not reached yet.
        /// </param>
        internal static IEnumerator SpendATurnClosingTheGap(
            MatchSceneCompositionRoot root, HeroId moverId, HeroId towardId,
            params CardDefinitionId[] reserved)
        {
            var runtime = root.Runtime;
            var hud = root.Hud;
            var towardAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(towardId)).Coordinate;
            if (HexDistanceQuery.Calculate(
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(moverId)).Coordinate,
                    towardAt) == 1)
            {
                yield break;
            }

            // MovingCardOf answers with a DISPLAY NAME, for the control label a journey clicks;
            // committing needs the definition's own id, so the card is chosen here on the same rule
            // (the highest-Initiative face printing a secondary Movement) and its id taken.
            var mover = HandOf(runtime, moverId)
                .Where(definition => !reserved.Contains(definition.Id))
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary))
                .OrderByDescending(definition => definition.Initiative.Value)
                .FirstOrDefault();
            Assert.That(mover, Is.Not.Null,
                moverId.Value + " holds no card printing a secondary Movement to close with.");

            // Committed seat by seat rather than through CommitSoTheNamedSeatActs, because that
            // helper spends every OTHER seat's lowest card and the reserve may be exactly that.
            UiPlayer.EndAnyTurnInProgress(runtime);
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
            {
                if (seat.Equals(moverId))
                {
                    Commit(runtime, seat, mover.Id);
                    continue;
                }

                var filler = HandOf(runtime, seat)
                    .Where(definition => !reserved.Contains(definition.Id))
                    .OrderBy(definition => definition.Initiative.Value)
                    .ThenBy(definition => definition.Id.Value, System.StringComparer.Ordinal)
                    .FirstOrDefault();
                Assert.That(filler, Is.Not.Null,
                    seat.Value + " holds nothing to commit beside the reserved face.");
                Commit(runtime, seat, filler.Id);
            }
            yield return YieldUntilTheNamedSeatActs(runtime, moverId);

            var movement = runtime.RoundView.Model.Actions.SingleOrDefault(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(movement, Is.Not.Null,
                "The committed card prints a secondary Movement to close the gap with. " +
                Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));

            var destination = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => HexDistanceQuery.Calculate(value.Destination, towardAt) == 1)
                .OrderBy(value => value.Cost)
                .Select(value => value.Destination)
                .First();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True,
                Remaining(runtime));
            yield return null;

            Assert.That(
                HexDistanceQuery.Calculate(
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(moverId)).Coordinate, towardAt),
                Is.EqualTo(1),
                "The mover is now adjacent, which is what an adjacent-target face needs. " +
                Remaining(runtime));

            yield return AdvanceToNextTurn(runtime);
        }

        internal static QuickGameHeroSetupState TaliSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == TaliCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Tali to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }
    }
}
