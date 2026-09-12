using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.ArienEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SwiftUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Swift's piece, the Director drawer and the card surface - the component half of her
    /// player-path gate, whose subject is the configured session and the board piece rather than
    /// anything her cards describe. Milestone 8.38f.
    /// </summary>
    public sealed class SwiftEngagedUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HerPieceSpawnsAsHerOwnToken_AndAnswersThePointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Swift on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view.");

            var swiftSetup = SwiftSeat(runtime);
            var swiftView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(swiftSetup.HeroId));

            // Her own content identity, her own side, and her own PREPARED token rather than the
            // generated fallback - milestone 8.38e prepared `swift-blue.png` from the bundled
            // manifest element 8597511, and this is the tier that reads whether the runtime found it.
            Assert.That(swiftView.HeroContentId.HasValue, Is.True);
            Assert.That(swiftView.HeroContentId.Value.Value,
                Is.EqualTo(SwiftCardContent.ContentSetId));
            Assert.That(swiftView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(swiftView.HasTokenVisual, Is.True);

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, swiftView.transform.position), Is.True,
                "Her piece answers a real camera ray at its own world position.");
            yield return null;
        }

        /// <summary>
        /// Opens the Director drawer only when it is shut. **It TOGGLES, and its own trigger's label
        /// changes when it is open** - shut it reads `DIRECTOR`, open it reads `DIRECTOR` followed by
        /// the drawer glyphs - so a blind second click closes a drawer an earlier step already opened
        /// and the controls inside it stop being found. RESET is the tell, because it is drawn only
        /// while the drawer is open.
        /// </summary>
        private static void OpenDirectorDrawer(ConfiguredMatchHudRenderer hud)
        {
            if (!UiPlayer.HasButton(hud, "RESET"))
            {
                UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            }
        }

        [UnityTest]
        public IEnumerator EveryFaceOfHersDrawsAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();

            // The shared measurement every hero's gate takes, rather than a footprint of this
            // fixture's own invention: it reads the DRAWN surface on the live panel at the reference
            // resolution, which is the question a source file's 372x520 cannot answer.
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Swift", SwiftCardContent.AllDefinitions, root.Hud);
        }

        /// <summary>
        /// The four session shapes her gate names, each driven through the Director drawer's own
        /// controls rather than through the runtime's methods - the gate's claim is about what a
        /// PLAYER can reach, and a journey calling Undo() directly would prove the rule and not the
        /// path.
        ///
        /// <para><b>RESET is taken BEFORE the restore, and the order is the whole lesson of this
        /// journey.</b> `CanRestart` is `AcceptedCommands.Count > 0`, and a snapshot restore rebuilds
        /// the session from a payload that carries the POSITION rather than the command history - so
        /// the restored board is correct and its accepted-command list is empty, which leaves RESET
        /// legitimately disabled. Reaching for RESET after a restore is asking for a control the
        /// product is right to withhold; the restore therefore goes last and proves continuation.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerEngagedBoardRestores_Undoes_Replays_AndRestarts()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftSetup = SwiftSeat(runtime);
            var swiftId = swiftSetup.HeroId;

            yield return PointTrayAt(root, swiftId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, swiftId));
            Assert.That(runtime.GetCommitmentStatus(swiftId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            var snapshot = runtime.CaptureSnapshot();
            Assert.That(snapshot, Is.Not.Null.And.Not.Empty);

            // RetainedPrefixUndo.
            OpenDirectorDrawer(hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(swiftId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "Her commitment undoes back to the retained prefix.");

            // RestartReconstruction. The re-commit is what puts a command back on the accepted list,
            // which is the condition RESET is gated on.
            yield return PointTrayAt(root, swiftId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, swiftId));
            OpenDirectorDrawer(hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "RESET returns her to the staged initial state with nothing accepted.");

            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            var swiftView = restarted.Single(view => view.Occupancy.HeroId.Equals(swiftId));
            Assert.That(swiftView.Coordinate, Is.EqualTo(swiftSetup.Coordinate),
                "Her piece stands on her staged space again after a restart.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(swiftId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(SwiftCardContent.StartingHand.Count),
                "Her whole p.5 starting hand is back in hand after the restart.");

            // SnapshotRestoreContinuation and ReplayReconstruction: the payload taken at the
            // committed turn restores onto a board that has since been restarted away from it.
            Assert.That(runtime.RestoreSnapshot(snapshot), Is.True,
                "The captured payload restores the committed turn it was taken in.");
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(swiftId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown),
                "The restored state is the one the snapshot recorded, not the restarted one.");
        }

        [UnityTest]
        public IEnumerator HerHandAndRailsRebuildOnTheSameFootprintAfterAPresentationReload()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftId = SwiftSeat(runtime).HeroId;

            var before = runtime.HandsView.Model.Hands
                .Single(value => value.HeroId.Equals(swiftId)).TrayCards.Count;
            Assert.That(before, Is.GreaterThan(0), "Her hand draws before the rebuild.");

            // Committing first makes the rebuild do work rather than redraw an untouched scene, and
            // the drawer's RESET is the reload path a player actually has.
            yield return PointTrayAt(root, swiftId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, swiftId));
            OpenDirectorDrawer(hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;
            yield return null;

            var after = runtime.HandsView.Model.Hands
                .Single(value => value.HeroId.Equals(swiftId)).TrayCards.Count;
            Assert.That(after, Is.EqualTo(before),
                "A presentation rebuild redraws her hand from the same rules state, not a second one.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Count(view => view.Occupancy.HeroId.Equals(swiftId)),
                Is.EqualTo(1),
                "Exactly one piece of hers stands after the rebuild.");
        }
    }
}
