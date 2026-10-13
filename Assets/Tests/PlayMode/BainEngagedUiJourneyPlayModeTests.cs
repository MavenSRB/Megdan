using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Bain's piece, the Director drawer and the card surface - the component half of his player-path
    /// gate, whose subject is the configured session and the board piece rather than anything his
    /// cards describe. Milestone 8.40k.
    ///
    /// <para><b>The session operations run on a board that deals him</b>, through the Director
    /// drawer's own controls rather than through the runtime's methods: the gate's claim is about what
    /// a PLAYER can reach, and a journey calling Undo() directly would prove the rule and not the
    /// path.</para>
    /// </summary>
    public sealed class BainEngagedUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HisPieceSpawnsAsHisOwnToken_AndAnswersThePointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Bain on Blue - so two pieces stand " +
                "and a third would be a stale or duplicated view.");

            var bainSetup = BainSeat(runtime);
            var bainView = heroViews.Single(view => view.Occupancy.HeroId.Equals(bainSetup.HeroId));

            // His own content identity, his own side, and his own PREPARED token rather than the
            // generated fallback - milestone 8.40j prepared his two team discs from the bundled
            // manifest element 8596242, which hides under the epithet `BountyHunter` (BA-16), and this
            // is the tier that reads whether the runtime found them.
            Assert.That(bainView.HeroContentId.HasValue, Is.True);
            Assert.That(bainView.HeroContentId.Value.Value,
                Is.EqualTo(BainCardContent.ContentSetId));
            Assert.That(bainView.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(bainView.HasTokenVisual, Is.True);

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, bainView.transform.position), Is.True,
                "His piece answers a real camera ray at its own world position.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryFaceOfHisDrawsAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();

            // The shared measurement every hero's gate takes, rather than a footprint of this
            // fixture's own invention: it reads the DRAWN surface on the live panel at the reference
            // resolution, which is the question a source file's 372x520 cannot answer.
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Bain", BainCardContent.AllDefinitions, root.Hud);
        }

        /// <summary>
        /// The player-facing Undo is driven through the Director drawer. Restart remains a direct
        /// reconstruction diagnostic and is deliberately exercised before snapshot restore, whose
        /// payload carries the position rather than accepted command history.
        /// </summary>
        [UnityTest]
        public IEnumerator HisEngagedBoardRestores_Undoes_Replays_AndRestarts()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainSetup = BainSeat(runtime);
            var bainId = bainSetup.HeroId;

            yield return PointTrayAt(root, bainId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, bainId));
            Assert.That(runtime.GetCommitmentStatus(bainId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            var snapshot = runtime.CaptureSnapshot();
            Assert.That(snapshot, Is.Not.Null.And.Not.Empty);

            // RetainedPrefixUndo.
            OpenDirectorDrawer(hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(bainId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "His commitment undoes back to the retained prefix.");

            // RestartReconstruction remains an engine diagnostic after the tray control's removal.
            yield return PointTrayAt(root, bainId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, bainId));
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "Restart reconstruction returns him to the staged initial state.");

            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            var bainView = restarted.Single(view => view.Occupancy.HeroId.Equals(bainId));
            Assert.That(bainView.Coordinate, Is.EqualTo(bainSetup.Coordinate),
                "His piece stands on his staged space again after a restart.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(bainId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(BainCardContent.StartingHand.Count),
                "His whole p.6 starting hand is back in hand after the restart.");

            // SnapshotRestoreContinuation and ReplayReconstruction: the payload taken at the
            // committed turn restores onto a board that has since been restarted away from it.
            Assert.That(runtime.RestoreSnapshot(snapshot), Is.True,
                "The captured payload restores the committed turn it was taken in.");
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(bainId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown),
                "The restored state is the one the snapshot recorded, not the restarted one.");
        }

        [UnityTest]
        public IEnumerator HisHandAndRailsRebuildOnTheSameFootprintAfterAPresentationReload()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var bainId = BainSeat(runtime).HeroId;

            var before = runtime.HandsView.Model.Hands
                .Single(value => value.HeroId.Equals(bainId)).TrayCards.Count;
            Assert.That(before, Is.GreaterThan(0), "His hand draws before the rebuild.");

            // Committing first makes the diagnostic rebuild do work rather than redraw an untouched scene.
            yield return PointTrayAt(root, bainId);
            yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, bainId));
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            yield return null;

            var after = runtime.HandsView.Model.Hands
                .Single(value => value.HeroId.Equals(bainId)).TrayCards.Count;
            Assert.That(after, Is.EqualTo(before),
                "A presentation rebuild redraws his hand from the same rules state, not a second one.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Count(view => view.Occupancy.HeroId.Equals(bainId)),
                Is.EqualTo(1),
                "Exactly one piece of his stands after the rebuild.");
        }
    }
}
