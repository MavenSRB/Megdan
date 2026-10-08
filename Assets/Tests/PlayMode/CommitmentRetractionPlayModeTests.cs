using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class CommitmentRetractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayerWithdrawsAndReplacesTheirCardThroughTheExistingUndoButton()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = ArienUiJourneyPlayModeTests.Root();
            var seat = root.Runtime.Host.Seats.Seats[0];
            MatchSceneCompositionRoot.NextComposedSeat = seat.Id;
            root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
            yield return null; yield return null;
            var runtime = root.Runtime; var host = runtime.Host;
            var card = runtime.HandsView.Model.ViewedHand.PlayableTrayCards.First().CardInstanceId;
            UiPlayer.Click(runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().First());
            yield return null; yield return null;
            UiPlayer.Click(root.Hud.RootElement.Q<Button>("gd-confirm-control"));
            yield return null; yield return null;
            Assert.That(host.Projection.GetCommitment(seat.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            var other = host.Seats.Seats.First(s => !s.Id.Equals(seat.Id));
            var otherCommand = host.ProjectionForSeat(other.Id).AuthorizedCommands.First(c => c.Kind == DirectorCommandKind.CommitCard);
            Assert.That(new SeatMatchClient(host.ClientForSeat(other.Id)).Submit(otherCommand).Accepted, Is.True);
            runtime.RefreshAll(); yield return null; yield return null;
            var undo = root.Hud.RootElement.Q<Button>("gd-seat-undo-control");
            Assert.That(UiPlayer.IsShown(undo), Is.True);
            Assert.That(undo.tooltip, Does.Contain("Withdraw"));
            yield return UiPlayer.CaptureScreen("tmp/982-commitment-undo.png");
            UiPlayer.Click(undo); yield return null; yield return null;
            Assert.That(host.Projection.GetCommitment(seat.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            Assert.That(host.Projection.GetCommitment(other.HeroId).Status, Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(runtime.ReadModel.Cards.Single(c => c.Id.Equals(card)).State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(runtime.HandsView.Model.ViewedHand.PlayableTrayCards.Any(c => c.CardInstanceId.Equals(card)), Is.True);
            UiPlayer.Click(runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().Last());
            yield return null; yield return null;
            UiPlayer.Click(root.Hud.RootElement.Q<Button>("gd-confirm-control"));
            yield return null; yield return null;
            foreach (var remaining in host.Seats.Seats.Where(s => host.Projection.GetCommitment(s.HeroId).Status == TurnCardCommitmentStatus.AwaitingResponse).ToArray())
            {
                var command = host.ProjectionForSeat(remaining.Id).AuthorizedCommands.First(c => c.Kind == DirectorCommandKind.CommitCard);
                Assert.That(new SeatMatchClient(host.ClientForSeat(remaining.Id)).Submit(command).Accepted, Is.True);
            }
            runtime.RefreshAll(); yield return null; yield return null;
            Assert.That(host.Projection.Commitments.All(c => c.Status == TurnCardCommitmentStatus.Revealed), Is.True);
            Assert.That(root.Hud.RootElement.Q<Button>("gd-seat-undo-control"), Is.Null);
            MatchSceneCompositionRoot.NextComposedSeat = null;
        }
    }
}
