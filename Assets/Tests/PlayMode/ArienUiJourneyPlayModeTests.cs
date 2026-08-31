using System.Collections;
using System.Linq;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Arien on the board a player actually loads: the configured match at rulebook p.6's spawns,
    /// driven through real pointer events and camera rays. Milestone 8.20a.
    ///
    /// <para>These are the journeys whose subject is the hero piece itself and the surface every
    /// turn starts from - the token standing where the setup put it, the tray it belongs to, the walk
    /// it takes, and the Director controls that put the match back. The journeys whose subject is a
    /// played card start from the engaged staging instead, in
    /// <see cref="ArienEngagedUiJourneyPlayModeTests"/>, for the reason milestone 8.15's own fixtures
    /// split the same way: the eight-turn approach march is somebody else's regression.</para>
    /// </summary>
    public sealed class ArienUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsAtTheBlueSpawnAsArienAndIsPointedAtByABoardClick()
        {
            yield return LoadConfiguredScene();
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "The staged match deals one hero a side, so two pieces stand on the board and a " +
                "third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var arienSetup = ArienSeat(runtime);
            var arienView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(arienSetup.HeroId));

            // Identity: the piece knows which printed set it is, wears the prepared official token
            // rather than the generated prism fallback, and reads the name a player sees.
            Assert.That(arienView.HeroContentId.HasValue, Is.True,
                "The staged Blue piece carries no content identity, so nothing on the board says " +
                "which hero it is.");
            Assert.That(arienView.HeroContentId.Value.Value,
                Is.EqualTo(ArienCardContent.ContentSetId));
            Assert.That(arienView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(arienView.HasTokenVisual, Is.True,
                "Arien's official circular token is prepared, so the piece must be wearing it " +
                "rather than the generated prism the fallback draws.");

            // Placement: the configured Blue hero spawn, and the space the authority agrees he is on.
            Assert.That(arienView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(arienView.ConfiguredSpawnPointId.Value,
                Is.EqualTo(arienSetup.SpawnPoint.SpawnPointId),
                "The piece must stand at the spawn point the configuration assigned it.");
            Assert.That(arienView.Coordinate, Is.EqualTo(arienSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(arienSetup.HeroId)).Coordinate,
                Is.EqualTo(arienView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            // The hand the piece is dealt is Arien's printed set and nobody else's, which is what
            // makes this piece this hero rather than a Blue-coloured one.
            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(arienSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(ArienCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()));

            // Pointer selection: a camera ray at the piece, past the HUD's own blocking answer.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var otherHeroId = runtime.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(arienSetup.HeroId)).HeroId;
            if (runtime.ViewedHeroId.Equals(arienSetup.HeroId))
            {
                var otherView = heroViews.Single(view => view.Occupancy.HeroId.Equals(otherHeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, otherView.transform.position),
                    Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(arienSetup.HeroId),
                "This journey points the tray away first, so the click under test is what brings " +
                "it back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, arienView.transform.position), Is.True,
                "The camera ray at Arien's own piece must be claimed by the selector chain.");
            yield return null;

            Assert.That(runtime.ViewedHeroId, Is.EqualTo(arienSetup.HeroId),
                "Clicking Arien's piece points the tray at Arien.");

            // Presentation rebuild: the whole visual layer is rebuilt from the current projection and
            // must come back as one piece per hero, in the same places, with no orphan left behind.
            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltArien = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(arienSetup.HeroId));
            Assert.That(rebuiltArien.Coordinate, Is.EqualTo(arienSetup.Coordinate));
            Assert.That(rebuiltArien.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(arienSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Arien_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace()
        {
            yield return LoadConfiguredScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var arienId = ArienSeat(runtime).HeroId;
            var otherId = runtime.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(arienId)).HeroId;

            // Arien commits the highest-Initiative card in his hand that prints a secondary Movement,
            // against the other hand's lowest, so he takes the first turn whichever seat the coin
            // flip opened the tray on.
            yield return PointTrayAt(root, arienId);
            yield return CommitThroughTray(root, MovingCardOf(runtime, arienId));
            yield return PointTrayAt(root, otherId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, otherId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(arienId),
                "Arien's higher Initiative gives him p.7's first turn.");
            Assert.That(runtime.GetCommitmentStatus(arienId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            var acceptedBeforeStart = runtime.AcceptedCommands.Count;
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBeforeStart + 1),
                "One click on the Movement control is one accepted command.");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "The clicked control must open p.10's movement session.");

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate;
            var destination = runtime.PendingMovementChoice.LegalDestinations
                .First(value => value.Cost == 1).Destination;
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement),
                "A destination a player is about to click must carry green corners before the click.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            // The piece follows the state: this is the hero-piece movement half of the row.
            var arienView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(arienId));
            Assert.That(arienView.Coordinate, Is.EqualTo(destination),
                "The token must be standing where the hero now is, not where he was.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            Assert.That(runtime.PendingMovementChoice, Is.Null);
            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(arienId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "Arien's spent card reads as this round's first turn on his own tray.");
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesAriensCommitmentAndRestartsHimOntoHisSpawn()
        {
            yield return LoadConfiguredScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var arienSetup = ArienSeat(runtime);
            yield return PointTrayAt(root, arienSetup.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, arienSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(arienSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(arienSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Arien's committed card.");

            // Commit again, then restart: the match returns to p.6's setup with Arien's own piece
            // back on his own spawn and no accepted history behind it.
            yield return PointTrayAt(root, arienSetup.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, arienSetup.HeroId));
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(arienSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(2),
                "A restart that leaves a duplicate piece behind is what this counts.");
            var arienView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(arienSetup.HeroId));
            Assert.That(arienView.Coordinate, Is.EqualTo(arienSetup.Coordinate),
                "Arien stands on his configured spawn again.");
            Assert.That(arienView.HasTokenVisual, Is.True,
                "and is still wearing his own official token.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(arienSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(ArienCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of him.");
        }

        /// <summary>
        /// Arien's half of milestone 8.20c's standard-footprint conformance: every one of his verified
        /// faces, drawn on the live panel at the validated player viewport, is exactly
        /// <see cref="CardFootprint"/> and shows its content whole.
        ///
        /// <para>The measurement is <see cref="StandardCardFootprintCheck"/>'s and is hero-neutral;
        /// this case supplies <see cref="ArienCardContent"/> and the panel. His set is the one that
        /// makes the check worth having on more than one hero: his longest printed sentence is longer
        /// than anything Wasp prints, and under the old floor-plus-stretch sizing that sentence was
        /// what decided how tall every card in a hand of his was.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryArienFaceAtTheStandardCardFootprint()
        {
            yield return LoadConfiguredScene();
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Arien", ArienCardContent.AllDefinitions, root.Hud);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The staged seat dealt Arien's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour - the seat is Blue today and the question this asks
        /// is which seat is Arien's.
        /// </summary>
        internal static QuickGameHeroSetupState ArienSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == ArienCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Arien to exactly one seat; it dealt him to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// Points the tray at one hero through a board click on that hero's own piece, which is the
        /// route a player has. A tray already pointing there is left alone.
        /// </summary>
        internal static IEnumerator PointTrayAt(MatchSceneCompositionRoot root, HeroId heroId)
        {
            if (root.Runtime.ViewedHeroId.Equals(heroId))
            {
                yield break;
            }

            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(candidate => candidate.Occupancy.HeroId.Equals(heroId));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointerInput, root.Hud, view.transform.position), Is.True,
                "The board click that points the tray at " + heroId.Value + " must be claimed.");
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(heroId));
        }

        internal static IEnumerator CommitThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            var hud = root.Hud;
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;
        }

        internal static CardDefinition[] HandOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .ToArray();

        internal static string MovingCardOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            var candidates = HandOf(runtime, heroId)
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary))
                .OrderByDescending(definition => definition.Initiative.Value)
                .ToArray();
            Assert.That(candidates, Is.Not.Empty,
                heroId.Value + " holds no card printing a secondary Movement.");
            return candidates[0].DisplayName.ToUpperInvariant();
        }

        internal static string LowestInitiativeCardOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            HandOf(runtime, heroId)
                .OrderBy(definition => definition.Initiative.Value)
                .First()
                .DisplayName
                .ToUpperInvariant();

        internal static ActionKind KindOf(
            ConfiguredMatchSceneRuntime runtime, ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        /// <summary>
        /// The staged Wasp-versus-Arien board these journeys were written on. Milestone 8.25 named it:
        /// it was the composition root default from 8.20 until then, when the default became rulebook
        /// p.5's six seats, and every case here counts the two pieces of a one-versus-one board.
        /// </summary>
        private static IEnumerator LoadConfiguredScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.VerifiedStagedArien;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        internal static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
