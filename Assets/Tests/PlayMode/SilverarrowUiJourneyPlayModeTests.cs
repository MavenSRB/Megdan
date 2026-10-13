using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
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
    /// Silverarrow the Pathfinder's piece, her tray and the Director drawer, driven through real
    /// pointer events and camera rays. Milestone 8.35z46, and the ninth hero to take
    /// the player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one, and for her it is not a convenience.</b>
    /// Rulebook p.5's six official seats are what <see cref="SceneCardContent.ConfiguredMatch"/> deals,
    /// and Silverarrow is not among them. Every earlier staged hero restages into milestone 8.15's
    /// engagement because two heroes fifteen spaces apart make a poor journey; for her the restaging
    /// is what makes her set REACHABLE AT ALL. RULING 1 gives six of her seven Attacks a target BAND -
    /// "at maximum range" means exactly the printed number, not a ceiling - so on the unrestaged board
    /// every Attack she prints has no legal target, and on the engaged one the two heroes stand two
    /// apart, which is inside her Range 2 basics and reachable from her Range 3 and 4 faces after one
    /// step. That measured fact decides where each of her journeys lives.</para>
    ///
    /// <para><b>She needs no ally arm.</b> Seventeen of her nineteen faces name an ENEMY or nobody at
    /// all. The two that print "a hero in radius" - Fae Healing and Nature's Blessing - are answered
    /// by RULING 8, which admits any hero including herself, so both already have two candidate
    /// classes on a one-seat-per-team board. The friendly-OTHER-hero arm is recorded as unreached by
    /// this staging rather than composed away.</para>
    ///
    /// <para><b>The driving helpers are shared rather than copied</b>, on the terms
    /// <see cref="DodgerUiJourneyPlayModeTests"/> records: the hero-agnostic steps live on the
    /// Xargatha fixtures and an eighth transcription of them would be an eighth place for one step to
    /// drift. What is Silverarrow's own is <see cref="SilverarrowSeat"/> and the journeys.</para>
    /// </summary>
    public sealed class SilverarrowUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnSilverarrowsOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Silverarrow on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var seat = SilverarrowSeat(runtime);
            var view = heroViews.Single(candidate => candidate.Occupancy.HeroId.Equals(seat.HeroId));

            Assert.That(view.HeroContentId.HasValue, Is.True,
                "The Blue piece carries no content identity, so nothing on the board says which hero " +
                "it is.");
            Assert.That(view.HeroContentId.Value.Value,
                Is.EqualTo(SilverarrowCardContent.ContentSetId),
                "The content identity is `silverarrow` and nothing else - SL-10 settled the spelling " +
                "from the two printed faces, and the epithet is a display label on the seat.");
            Assert.That(view.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(view.HasTokenVisual, Is.True,
                "Silverarrow's official circular token is prepared, so the piece must be wearing it " +
                "rather than the generated prism the fallback draws.");
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(seat.HeroId)).DisplayName,
                Is.EqualTo("Silverarrow the Pathfinder"),
                "The seat carries the name her own dashboard prints.");

            Assert.That(view.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(view.ConfiguredSpawnPointId.Value.Value, Is.EqualTo("blue-hero-01"),
                "She takes the map's own first Blue spawn point rather than a space invented for " +
                "her; the staging moves the piece and leaves the spawn where p.6 declared it.");
            Assert.That(view.Coordinate, Is.EqualTo(seat.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(seat.HeroId)).Coordinate,
                Is.EqualTo(view.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            // Eighteen faces, because p.20's Handicap card - Keep the Distance - is in the box.
            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(seat.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(SilverarrowCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()));

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(seat.HeroId))
            {
                var other = heroViews.First(candidate =>
                    !candidate.Occupancy.HeroId.Equals(seat.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(seat.HeroId),
                "This journey points the tray away first, so the click under test is what brings it " +
                "back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, view.transform.position), Is.True,
                "The camera ray at her piece must be claimed by the board rather than swallowed.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId));

            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltSilverarrow = rebuilt.Single(candidate =>
                candidate.Occupancy.HeroId.Equals(seat.HeroId));
            Assert.That(rebuiltSilverarrow.Coordinate, Is.EqualTo(seat.Coordinate));
            Assert.That(rebuiltSilverarrow.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Silverarrow_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;

            // Trailblazer is the one card of hers this step can never pick, and that is content: it
            // is the only face in her set printing no secondary Movement icon at all, which the
            // glyph-shape pass recorded and a positional read would have got backwards.
            yield return CommitEverySeat(root, silverarrowId, MovingCardOf(runtime, silverarrowId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(silverarrowId),
                "Her Gold basic prints Initiative 12, which is p.7's first turn on this staging.");
            Assert.That(runtime.GetCommitmentStatus(silverarrowId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            var acceptedBeforeStart = runtime.AcceptedCommands.Count;
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBeforeStart + 1),
                "One click on the Movement control is one accepted command.");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableStep(pointerInput, runtime);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(candidate => candidate.Occupancy.HeroId.Equals(silverarrowId));
            Assert.That(view.Coordinate, Is.EqualTo(destination),
                "The token must be standing where the hero now is, not where she was.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            Assert.That(runtime.PendingMovementChoice, Is.Null);
            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(silverarrowId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesSilverarrowsCommitmentAndRestartsHerOntoHerStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var seat = SilverarrowSeat(runtime);
            yield return PointTrayAt(root, seat.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat.HeroId));
            Assert.That(runtime.GetCommitmentStatus(seat.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(seat.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Silverarrow's committed card.");

            yield return PointTrayAt(root, seat.HeroId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat.HeroId));
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(seat.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(2));
            var view = restarted.Single(candidate =>
                candidate.Occupancy.HeroId.Equals(seat.HeroId));
            Assert.That(view.Coordinate, Is.EqualTo(seat.Coordinate));
            Assert.That(view.HasTokenVisual, Is.True);
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(seat.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(SilverarrowCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of her.");
        }

        /// <summary>
        /// Silverarrow's half of milestone 8.20c's standard-footprint conformance: every one of her
        /// verified faces, drawn one at a time on the live HUD panel and measured against
        /// <c>CardFootprint</c>.
        ///
        /// <para>Her set is a real test of it. Three of her Red faces print rulebook p.10's modal form
        /// with two bullets, both Gold basics print a sentence and an "After the attack:" rider, and
        /// Rain of Arrows prints a targeting sentence and a two-clause chain on one face - the longest
        /// printed text in her set.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEverySilverarrowFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Silverarrow", SilverarrowCardContent.AllDefinitions, root.Hud);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The staged seat dealt Silverarrow's printed set, found by the content identity the
        /// configuration bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState SilverarrowSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == SilverarrowCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Silverarrow to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// The first one-cost movement destination whose own camera ray reaches the board rather than
        /// a hero piece standing over it.
        /// </summary>
        private static HexCoordinate FirstClickableStep(
            PrimitiveHeroPointerInput pointerInput, ConfiguredMatchSceneRuntime runtime)
        {
            var steps = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(steps, Is.Not.Empty, "The opened movement session offers no one-space step.");
            return FirstClickableSpace(pointerInput, steps);
        }
    }
}
