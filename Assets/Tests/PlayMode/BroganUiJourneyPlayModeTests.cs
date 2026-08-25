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
    /// Brogan on the board at rulebook p.6's spawns, driven through real pointer events and camera
    /// rays. Milestone 8.21.
    ///
    /// <para><b>Why this fixture's scene has three heroes.</b> Wasp and Arien are what
    /// <see cref="SceneCardContent.ConfiguredMatch"/> deals, so there is no unrestaged composition in
    /// which Brogan stands alone at a spawn; the three-hero staging is the one that puts his own token
    /// on his own declared spawn point. It is also the only composition two of his faces can be played
    /// in at all - War Drummer and Master Skald name a friendly hero, and rulebook p.10 keeps the
    /// acting hero out of his own action - which is why the two ally journeys are here rather than in
    /// the progressed fixture their Tiers would otherwise put them in.</para>
    ///
    /// <para>The journeys whose subject is a played card against an enemy start from the engaged
    /// staging instead, in <see cref="BroganEngagedUiJourneyPlayModeTests"/> and
    /// <see cref="BroganProgressedUiJourneyPlayModeTests"/>, for the reason milestone 8.15's own
    /// fixtures split the same way: the approach march is somebody else's regression.</para>
    /// </summary>
    public sealed class BroganUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsAtTheSecondBlueSpawnAsBroganAndIsPointedAtByABoardClick()
        {
            yield return LoadAllyScene(SceneCardContent.VerifiedStagedBroganWithAlly);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(3),
                "This staging deals three heroes - Wasp on Red, Arien and Brogan on Blue - so three " +
                "pieces stand and a fourth would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var broganSetup = BroganSeat(runtime);
            var broganView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(broganSetup.HeroId));

            // Identity: the seat the scene authored no view for still knows which printed set it is,
            // wears the prepared official token rather than the generated prism fallback, and stands
            // on the Blue side. This is the half `EnsureViewsForConfiguredSeats` is responsible for.
            Assert.That(broganView.HeroContentId.HasValue, Is.True,
                "The generated third piece carries no content identity, so nothing on the board " +
                "says which hero it is.");
            Assert.That(broganView.HeroContentId.Value.Value,
                Is.EqualTo(BroganCardContent.ContentSetId));
            Assert.That(broganView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(broganView.HasTokenVisual, Is.True,
                "Brogan's official circular token is prepared, so the piece must be wearing it " +
                "rather than the generated prism the fallback draws.");

            // Placement: the second Blue hero spawn the reviewed map has declared all along.
            Assert.That(broganView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(broganView.ConfiguredSpawnPointId.Value.Value, Is.EqualTo("blue-hero-02"),
                "Brogan takes the map's own second Blue spawn point rather than a space invented " +
                "for him.");
            Assert.That(broganView.Coordinate, Is.EqualTo(broganSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(broganSetup.HeroId)).Coordinate,
                Is.EqualTo(broganView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            // The hand the piece is dealt is Brogan's printed set and nobody else's.
            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(broganSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(BroganCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()));

            // Pointer selection: a camera ray at the piece, past the HUD's own blocking answer. The
            // generated seat also had to be given a selector, or this click reaches the board behind
            // him and the tray never arrives.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(broganSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(broganSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(broganSetup.HeroId),
                "This journey points the tray away first, so the click under test is what brings " +
                "it back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, broganView.transform.position), Is.True,
                "The camera ray at Brogan's own piece must be claimed by the selector chain.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(broganSetup.HeroId),
                "Clicking Brogan's piece points the tray at Brogan.");

            // Presentation rebuild: the whole visual layer is rebuilt from the current projection and
            // must come back as one piece per hero - including the generated one, which is the piece
            // a rebuild could most easily lose or duplicate.
            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(3),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltBrogan = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(broganSetup.HeroId));
            Assert.That(rebuiltBrogan.Coordinate, Is.EqualTo(broganSetup.Coordinate));
            Assert.That(rebuiltBrogan.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(broganSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Brogan_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace()
        {
            yield return LoadAllyScene(SceneCardContent.VerifiedStagedBroganWithAlly);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;

            // Brogan commits the highest-Initiative card in his hand that prints a secondary Movement
            // icon, and the other two seats commit their lowest, so he takes the first turn whichever
            // seat the tray opened on. Rulebook p.7 resolves the higher printed Initiative first.
            yield return CommitEverySeat(root, broganId, MovingCardOf(runtime, broganId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Brogan's higher Initiative gives him p.7's first turn.");
            Assert.That(runtime.GetCommitmentStatus(broganId),
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

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableStep(pointerInput, runtime);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.True,
                "A destination a player is about to click must be lit before they click it.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            // The piece follows the state: this is the hero-piece movement half of the row, and on a
            // generated view it is the half that would silently not happen.
            var broganView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(broganId));
            Assert.That(broganView.Coordinate, Is.EqualTo(destination),
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
                .Single(hand => hand.HeroId.Equals(broganId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "Brogan's spent card reads as this round's first turn on his own tray.");
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesBrogansCommitmentAndRestartsHimOntoHisSpawn()
        {
            yield return LoadAllyScene(SceneCardContent.VerifiedStagedBroganWithAlly);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var broganSetup = BroganSeat(runtime);
            yield return PointTrayAt(root, broganSetup.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, broganSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(broganSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            // The drawer relabels its own toggle when open, so it is found by label prefix.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(broganSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Brogan's committed card.");

            yield return PointTrayAt(root, broganSetup.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, broganSetup.HeroId));
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(broganSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(3),
                "A restart that leaves a duplicate piece behind is what this counts, and the " +
                "generated third seat is the one it could lose.");
            var broganView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(broganSetup.HeroId));
            Assert.That(broganView.Coordinate, Is.EqualTo(broganSetup.Coordinate),
                "Brogan stands on his configured spawn again.");
            Assert.That(broganView.HasTokenVisual, Is.True,
                "and is still wearing his own official token.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(broganSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(BroganCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of him.");
        }

        /// <summary>
        /// Brogan's half of milestone 8.20c's standard-footprint conformance: every one of his verified
        /// faces, drawn on the live panel at the validated player viewport, is exactly
        /// <see cref="CardFootprint"/> and shows its content whole.
        ///
        /// <para>The measurement is <see cref="StandardCardFootprintCheck"/>'s and is hero-neutral;
        /// this case supplies <see cref="BroganCardContent"/> and the panel. It is the check the
        /// milestone 8.20c gate exists for: a hero added after the footprint became a standard has to
        /// fit it rather than widen it, and Brogan's modal faces print the two longest bulleted
        /// sentences in the build.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryBroganFaceAtTheStandardCardFootprint()
        {
            yield return LoadAllyScene(SceneCardContent.VerifiedStagedBroganWithAlly);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Brogan", BroganCardContent.AllDefinitions, root.Hud);
        }

        /// <summary>
        /// War Drummer, the first of the two faces the ally seat exists for: "A friendly hero in range
        /// gains 1 coin; if any hero was defeated this round, that friendly hero gains 3 coins,
        /// instead."
        ///
        /// <para>The friendly hero is chosen by clicking the piece where it stands, and the actor is
        /// not among the candidates - p.10 keeps the acting hero out of his own action - which is
        /// exactly why this card needs a second hero on Brogan's side to be playable at all. Nobody
        /// has been defeated in a match one turn old, so the printed rider does not fire and the coin
        /// count moves by one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator WarDrummer_GivesTheAlliedHeroOneCoinChosenByAClickOnHisPiece()
        {
            yield return LoadAllyScene(SceneCardContent.VerifiedStagedBroganWithAllyAtTierTwo);
            yield return AllyCoinGrant(BroganCardContent.WarDrummer, 1);
        }

        /// <summary>
        /// Master Skald, the Tier III half of the same pair: "A friendly hero in range gains 2 coins;
        /// if any hero was defeated this round, that friendly hero gains 4 coins instead."
        ///
        /// <para>Driven as its own instance rather than left to its sibling's journey, on the terms
        /// milestone 8.15 set for Electrocute beside Shock: the number is read off the played card,
        /// and a face proved only by the card above it in the upgrade pair is a face nobody has
        /// played.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MasterSkald_GivesTheAlliedHeroTwoCoinsThroughTheSameBoardClick()
        {
            yield return LoadAllyScene(SceneCardContent.VerifiedStagedBroganWithAllyAtTierThree);
            yield return AllyCoinGrant(BroganCardContent.MasterSkald, 2);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// One printed "A friendly hero in range gains N coins" face, played from Brogan's own tray
        /// control and answered by clicking the ally's piece, with the granted amount read off the
        /// immutable progression projection before and after.
        /// </summary>
        private static IEnumerator AllyCoinGrant(CardDefinitionId cardId, int expectedCoins)
        {
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var allyId = runtime.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue && !hero.HeroId.Equals(broganId))
                .HeroId;

            yield return CommitEverySeat(root, broganId, NameOf(runtime, cardId));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "The other two seats committed their lowest cards, so p.7 gives Brogan this turn.");

            var coinsBefore = runtime.CoinsOf(allyId);
            var actorCoinsBefore = runtime.CoinsOf(broganId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed Skill must stop and ask which friendly hero in range gains the coins.");
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Has.Count.EqualTo(1),
                "The one candidate is the ally: p.10 keeps the acting hero out of his own action, " +
                "and the enemy hero is not friendly. Offered: " +
                string.Join(", ", candidates.Select(value => value.Space.ToString())));

            var allyView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(allyId));
            Assert.That(allyView.Coordinate, Is.EqualTo(candidates[0].Space),
                "The offered candidate is the space the allied hero is standing on.");
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, allyView.transform.position), Is.True,
                "The friendly hero is chosen by clicking his piece where it stands.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The one question the card asks is answered. Remaining: " +
                BroganEngagedUiJourneyPlayModeTests.Remaining(runtime));
            Assert.That(runtime.CoinsOf(allyId), Is.EqualTo(coinsBefore + expectedCoins),
                "The clicked ally gains exactly the printed amount; no hero has been defeated this " +
                "round, so the card's rider does not raise it.");
            Assert.That(runtime.CoinsOf(broganId), Is.EqualTo(actorCoinsBefore),
                "and the acting hero gains nothing, because the card names somebody else.");
        }

    

        internal static string NameOf(ConfiguredMatchSceneRuntime runtime, CardDefinitionId cardId) =>
            runtime.Definitions
                .Single(definition => definition.Id.Equals(cardId))
                .DisplayName
                .ToUpperInvariant();

        /// <summary>
        /// The staged seat dealt Brogan's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering - two Blue seats stand in this
        /// composition and the question this asks is which of them is Brogan's.
        /// </summary>
        internal static QuickGameHeroSetupState BroganSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == BroganCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Brogan to exactly one seat; it dealt him to " +
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
        /// a hero piece standing over it. Three pieces stand in this composition, so a step chosen by
        /// board order alone is more likely to be occluded than in a two-hero one.
        /// </summary>
        private static GuardsDigital.Core.Board.HexCoordinate FirstClickableStep(
            PrimitiveHeroPointerInput pointerInput, ConfiguredMatchSceneRuntime runtime)
        {
            var steps = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(steps, Is.Not.Empty, "The opened movement session offers no one-space step.");
            return BroganEngagedUiJourneyPlayModeTests.FirstClickableSpace(pointerInput, steps);
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

        internal static IEnumerator LoadAllyScene(SceneCardContent content)
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
