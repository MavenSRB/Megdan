using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;
namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExperimentalCoordinationPresentationTests
    {
        [Test] public void AlliedInspectionHasCardsButNoGameplayControlsAndCannotInspectEnemies()
        {
            var state = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var host = MatchHost.Compose(new MatchSessionInitialState(state.Scenario, state.Setup, state.AvailableMaps,
                state.InitialConfiguration, experiments: new ExperimentalMatchOptions(teammateCoordination: true)));
            var red = host.Seats.ForSide(TeamSide.Red).ToArray();
            var view = new DirectorHandsUnityView();
            var presenter = new DirectorHandsPresenter(new SeatMatchClient(host.ClientForSeat(red[0].Id)), view, red[0].HeroId);
            Assert.That(presenter.SelectViewedHero(red[1].HeroId), Is.True);
            Assert.That(view.Model.ViewedHand.IsReadOnly, Is.True);
            Assert.That(view.Model.ViewedHand.PlayableTrayCards, Is.Not.Empty);
            Assert.That(view.Model.ViewedHand.PlayableTrayCards.Any(c => c.IsSelectable), Is.False);
            Assert.That(view.Model.ViewedHand.CanPass, Is.False);
            var card = view.Model.ViewedHand.Cards.First();
            Assert.That(presenter.SelectCommitmentCard(card.Id), Is.False);
            Assert.That(presenter.CommitCardImmediately(card.Id), Is.False);
            Assert.That(presenter.SelectViewedHero(host.Seats.ForSide(TeamSide.Blue).First().HeroId), Is.False);
            Assert.That(view.Root.Q<Button>("my-cards"), Is.Not.Null);
            Assert.That(presenter.SelectViewedHero(red[0].HeroId), Is.True);
            Assert.That(view.Model.ViewedHand.IsReadOnly, Is.False);
            Assert.That(host.Version, Is.Zero);
        }
        [Test] public void FreshHostMenuResetsExperimentsButRefreshDoesNotEraseTheChoice()
        {
            var view = new PrivateLobbyUnityView();
            view.ShowPage(PrivateLobbyEntryPage.Host);
            var toggle = view.Root.Q<Toggle>("experimental-coordination");
            Assert.That(toggle, Is.Not.Null);
            toggle.value = true;
            Assert.That(view.CurrentEntry().Experiments.TeammateCoordination, Is.True);
            view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(view.CurrentEntry().Experiments.TeammateCoordination, Is.True);
            view.ShowPage(PrivateLobbyEntryPage.Landing);
            view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(view.CurrentEntry().Experiments.TeammateCoordination, Is.False);
            Assert.That(view.CurrentEntry().Experiments.TurnTimer, Is.False);
        }
    }
}
