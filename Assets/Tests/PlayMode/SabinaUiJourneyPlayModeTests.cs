using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
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
    /// Sabina's piece, her tray and the Director drawer, driven through real pointer events and camera
    /// rays. Milestone 8.22.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Wasp and Arien are what
    /// <see cref="SceneCardContent.ConfiguredMatch"/> deals, so there is no composition in which Sabina
    /// stands unrestaged at her spawn - every arm that deals her restages both heroes into the
    /// engagement milestone 8.15 built. The piece rows below therefore prove the spawn the two ways
    /// that staging leaves open: the token carries the map's own declared Blue spawn point and stands
    /// exactly where the configured setup says it does, and p.13's respawn puts her back on that spawn
    /// from a board click, which is
    /// <see cref="SabinaEngagedUiJourneyPlayModeTests.Sabina_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick"/>'s
    /// subject.</para>
    ///
    /// <para><b>She needs no ally arm, unlike Brogan.</b> Two of his faces name a friendly hero and a
    /// one-hero team gave that selection no candidate; hers command minions, and p.8's waves put twelve
    /// of those on this board before anybody acts. The one-hero-per-team staging is enough for every
    /// face she prints.</para>
    ///
    /// <para>The journeys whose subject is a played card start from the same staging in
    /// <see cref="SabinaEngagedUiJourneyPlayModeTests"/> and, at rulebook p.15's later stages, in
    /// <see cref="SabinaProgressedUiJourneyPlayModeTests"/>.</para>
    /// </summary>
    public sealed class SabinaUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnSabinasOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Sabina on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var sabinaSetup = SabinaSeat(runtime);
            var sabinaView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(sabinaSetup.HeroId));

            // Identity: the seat knows which printed set it is, wears the prepared official token
            // rather than the generated prism fallback, and stands on the Blue side.
            Assert.That(sabinaView.HeroContentId.HasValue, Is.True,
                "The Blue piece carries no content identity, so nothing on the board says which " +
                "hero it is.");
            Assert.That(sabinaView.HeroContentId.Value.Value,
                Is.EqualTo(SabinaCardContent.ContentSetId),
                "The content identity is `sabina` and nothing else - the epithet S-01 answered is a " +
                "display label on the seat, and no piece, rule or token is addressed by it.");
            Assert.That(sabinaView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(sabinaView.HasTokenVisual, Is.True,
                "Sabina's official circular token is prepared, so the piece must be wearing it " +
                "rather than the generated prism the fallback draws.");
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(sabinaSetup.HeroId)).DisplayName,
                Is.EqualTo("Sabina the Gunslinger"),
                "The seat carries the rulebook's setup-step-7 name the user selected for S-01.");

            // Placement: the map's own first Blue spawn point, and the space the staging put her on.
            Assert.That(sabinaView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(sabinaView.ConfiguredSpawnPointId.Value.Value, Is.EqualTo("blue-hero-01"),
                "Sabina takes the map's own first Blue spawn point rather than a space invented " +
                "for her; the staging moves the piece and leaves the spawn where p.6 declared it.");
            Assert.That(sabinaView.Coordinate, Is.EqualTo(sabinaSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaSetup.HeroId)).Coordinate,
                Is.EqualTo(sabinaView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            // The hand the piece is dealt is Sabina's printed set and nobody else's.
            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(sabinaSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(SabinaCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()));

            // Pointer selection: a camera ray at the piece, past the HUD's own blocking answer.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(sabinaSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(sabinaSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(sabinaSetup.HeroId),
                "This journey points the tray away first, so the click under test is what brings " +
                "it back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, sabinaView.transform.position), Is.True,
                "The camera ray at Sabina's own piece must be claimed by the selector chain.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(sabinaSetup.HeroId),
                "Clicking Sabina's piece points the tray at Sabina.");

            // Presentation rebuild: the whole visual layer is rebuilt from the current projection and
            // must come back as one piece per hero.
            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltSabina = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(sabinaSetup.HeroId));
            Assert.That(rebuiltSabina.Coordinate, Is.EqualTo(sabinaSetup.Coordinate));
            Assert.That(rebuiltSabina.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(sabinaSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Sabina_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;

            // Sabina commits the highest-Initiative card in her hand that prints a secondary Movement
            // icon and the enemy commits their lowest, so she takes the first turn whichever seat the
            // tray opened on. Rulebook p.7 resolves the higher printed Initiative first.
            yield return CommitEverySeat(root, sabinaId, MovingCardOf(runtime, sabinaId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId),
                "Sabina's higher Initiative gives her p.7's first turn.");
            Assert.That(runtime.GetCommitmentStatus(sabinaId),
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

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableStep(pointerInput, runtime);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.True,
                "A destination a player is about to click must be lit before they click it.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            // The piece follows the state: this is the hero-piece movement half of the row.
            var sabinaView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(sabinaId));
            Assert.That(sabinaView.Coordinate, Is.EqualTo(destination),
                "The token must be standing where the hero now is, not where she was.");

            // Point Blank Shot prints one space of secondary Movement, so the walk that spends it
            // closes the session on its own; a card printing more leaves it open for a second step and
            // the stop control is what ends the turn.
            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            Assert.That(runtime.PendingMovementChoice, Is.Null);
            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(sabinaId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "Sabina's spent card reads as this round's first turn on her own tray.");
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesSabinasCommitmentAndRestartsHerOntoHerStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var sabinaSetup = SabinaSeat(runtime);
            yield return PointTrayAt(root, sabinaSetup.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, sabinaSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(sabinaSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            // The drawer relabels its own toggle when open, so it is found by label prefix.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(sabinaSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Sabina's committed card.");

            yield return PointTrayAt(root, sabinaSetup.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, sabinaSetup.HeroId));
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(sabinaSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(2),
                "A restart that leaves a duplicate piece behind is what this counts.");
            var sabinaView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(sabinaSetup.HeroId));
            Assert.That(sabinaView.Coordinate, Is.EqualTo(sabinaSetup.Coordinate),
                "Sabina stands on her configured space again.");
            Assert.That(sabinaView.HasTokenVisual, Is.True,
                "and is still wearing her own official token.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(sabinaSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(SabinaCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of her.");
        }

        /// <summary>
        /// Sabina's half of milestone 8.20c's standard-footprint conformance: every one of her verified
        /// faces, drawn one at a time on the live HUD panel and measured against
        /// <c>CardFootprint</c>. The check is hero-neutral and enumerates the hero's own content, which
        /// this case supplies. It is the check the milestone 8.20c gate exists for: a hero added after
        /// the footprint became a standard has to fit it rather than widen it, and Point Blank Shot
        /// carries the build's only parenthetical push reminder.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEverySabinaFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Sabina", SabinaCardContent.AllDefinitions, root.Hud);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        internal static string NameOf(ConfiguredMatchSceneRuntime runtime, CardDefinitionId cardId) =>
            runtime.Definitions
                .Single(definition => definition.Id.Equals(cardId))
                .DisplayName
                .ToUpperInvariant();

        /// <summary>
        /// The staged seat dealt Sabina's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering. It is the content id that is
        /// looked up and never the display name: S-01's epithet governs the label alone.
        /// </summary>
        internal static QuickGameHeroSetupState SabinaSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == SabinaCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Sabina to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// Commits one named card out of the named hero's own tray and the lowest-Initiative card out
        /// of every other seat's, so p.7 opens the turn on the hero the journey is about.
        /// </summary>
        internal static IEnumerator CommitEverySeat(
            MatchSceneCompositionRoot root, HeroId actingHeroId, string printedName)
        {
            var runtime = root.Runtime;
            yield return PointTrayAt(root, actingHeroId);
            yield return CommitThroughTray(root, printedName);

            foreach (var seat in runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(actingHeroId))
                .Select(hero => hero.HeroId)
                .ToArray())
            {
                yield return PointTrayAt(root, seat);
                yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }
        }

        /// <summary>
        /// The first one-cost movement destination whose own camera ray reaches the board rather than
        /// a hero piece standing over it.
        /// </summary>
        private static GuardsDigital.Core.Board.HexCoordinate FirstClickableStep(
            PrimitiveHeroPointerInput pointerInput, ConfiguredMatchSceneRuntime runtime)
        {
            var steps = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(steps, Is.Not.Empty, "The opened movement session offers no one-space step.");
            return SabinaEngagedUiJourneyPlayModeTests.FirstClickableSpace(pointerInput, steps);
        }

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

        internal static IEnumerator StartActionThroughControl(
            MatchSceneCompositionRoot root, ActionKind kind)
        {
            yield return null;
            var runtime = root.Runtime;
            var action = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == kind);
            UiPlayer.Click(UiPlayer.Button(root.Hud, action.Label));
            yield return null;
        }

        internal static IEnumerator LoadStagedScene(SceneCardContent content)
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent = content;
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
