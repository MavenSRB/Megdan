using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;
namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExperimentalCoordinationPresentationTests
    {
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void SpellbookFacesFollowTheSameTeamAndEnemyOptions(bool allies, bool enemies)
        {
            var host = HeroCardInspectionTests.Create(allies, enemies);
            var hero = host.Projection.Setup.Heroes.Single(h => h.ContentId.Value == "gydion");
            var ownerSeat = host.Seats.Seats.Single(seat => seat.Owns(hero.HeroId));
            var ownerBook = SpellbookPresentation.ForHero(host.ProjectionForSeat(ownerSeat.Id), hero.HeroId, "Gydion");
            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(NetworkSeatProjectionCodec.Encode(projection), out var record), Is.True);
                var network = new NetworkMatchReadModel(host.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
                foreach (IMatchReadModel view in new IMatchReadModel[] { projection, network })
                {
                    var book = SpellbookPresentation.ForHero(view, hero.HeroId, "Gydion");
                    bool allowed = seat.Owns(hero.HeroId) || (seat.TeamSide == hero.TeamSide ? allies : enemies);
                    Assert.That(book.InBookFaces.Count, Is.EqualTo(allowed ? 22 : 0));
                    Assert.That(book.InBookBacks.Sum(b => b.Count), Is.EqualTo(allowed ? 0 : 22));
                    Assert.That(book.Subtitle.Contains("Only you"), Is.False, "The owner-only subtitle is no longer true under sharing.");
                    if (allowed)
                    {
                        Assert.That(book.InBookFaces.Select(face => face.DefinitionReference),
                            Is.EqualTo(ownerBook.InBookFaces.Select(face => face.DefinitionReference)));
                        Assert.That(book.RemovedFaces.Select(face => face.DefinitionReference),
                            Is.EqualTo(ownerBook.RemovedFaces.Select(face => face.DefinitionReference)));
                        Assert.That(book.Title, Is.EqualTo(ownerBook.Title));
                        Assert.That(book.Subtitle, Is.EqualTo(ownerBook.Subtitle));
                    }
                }
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void PortraitHasNoFiltersAndHiddenCommitmentPreservesEveryFaceAndItsOrder(bool useArtwork)
        {
            using var artwork = ConfiguredCardArtwork.UseTextureLoader(_ => useArtwork ? UnityEngine.Texture2D.blackTexture : null);
            var host = HeroCardInspectionTests.Create(true, true);
            var owner = host.Seats.Seats[0];
            var viewer = host.Seats.Seats.Last();
            IMatchReadModel current = host.ProjectionForSeat(viewer.Id);
            var view = new HeroCardInspectionUnityView(() => current);
            Assert.That(view.Open(owner.HeroId, "Inspected hero"), Is.True);
            Assert.That(view.Root.Query<DropdownField>().ToList(), Is.Empty);
            Assert.That(view.Root.Q("gd-hero-card-filters"), Is.Null);
            var faces = view.Root.Query<VisualElement>("gd-inspected-card").ToList();
            Assert.That(faces.Count, Is.EqualTo(5));
            foreach (var face in faces)
            {
                Assert.That(face.Q("gd-card-history"), Is.Null, "Generated faces must omit lifecycle captions too.");
                var image = face.Q("gd-card-artwork");
                Assert.That(image != null, Is.EqualTo(useArtwork));
                if (image != null) Assert.That(image.tooltip, Is.EqualTo(face.tooltip), "Artwork tooltips show only the card name.");
            }
            var own = host.ProjectionForSeat(owner.Id);
            var command = own.AuthorizedCommands.First(c => c.Kind == DirectorCommandKind.CommitCard);
            Assert.That(host.SubmitForSeat(owner.Id, new SeatSubmission(new SubmissionId("inspection-face-retention"),
                own.Version, command)).Accepted, Is.True);
            current = host.ProjectionForSeat(viewer.Id);
            view.Refresh();
            Assert.That(view.Root.Query<VisualElement>("gd-inspected-card").ToList(), Is.EqualTo(faces),
                "Committing a hidden card cannot remove, reorder, rebuild or mark any displayed face.");
        }
        [Test] public void EnemySettingIsIndependentAndResetsOnANewHostMenu()
        {
            var view = new PrivateLobbyUnityView(); view.ShowPage(PrivateLobbyEntryPage.Host);
            var enemy = view.Root.Q<Toggle>("experimental-enemy-info");
            Assert.That(enemy.value, Is.False);
            enemy.value = true;
            Assert.That(view.CurrentEntry().Experiments.EnemyCardInformation, Is.True);
            Assert.That(view.CurrentEntry().Experiments.TeammateCoordination, Is.False);
            Assert.That(view.CurrentEntry().Experiments.TurnTimer, Is.False);
            view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(enemy.value, Is.True);
            view.ShowPage(PrivateLobbyEntryPage.Landing); view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(enemy.value, Is.False);
        }
        [Test] public void MinionBattleCapAcceptsZeroAndResetsDisabledForANewHostMenu()
        {
            var view = new PrivateLobbyUnityView();
            view.ShowPage(PrivateLobbyEntryPage.Host);
            var toggle = view.Root.Q<Toggle>("experimental-minion-battle-cap");
            var field = view.Root.Q<IntegerField>("experimental-minion-battle-cap-value");
            Assert.That(toggle.value, Is.False);
            Assert.That(field.value, Is.EqualTo(1));
            toggle.value = true;
            field.value = 0;
            Assert.That(view.CurrentEntry().Experiments.MinionBattleRemovalCapEnabled, Is.True);
            Assert.That(view.CurrentEntry().Experiments.MinionBattleRemovalCap, Is.Zero);
            view.ShowPage(PrivateLobbyEntryPage.Landing);
            view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(toggle.value, Is.False);
            Assert.That(field.value, Is.EqualTo(1));
            Assert.That(view.CurrentEntry().Experiments.MinionBattleRemovalCapEnabled, Is.False);
        }
        [Test] public void OneLifeHeroBaseToggleIsIndependentAndResetsForANewHostMenu()
        {
            var view = new PrivateLobbyUnityView();
            view.ShowPage(PrivateLobbyEntryPage.Host);
            var toggle = view.Root.Q<Toggle>("experimental-hero-defeat-base-one");
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.value, Is.False);
            toggle.value = true;
            Assert.That(view.CurrentEntry().Experiments.HeroDefeatBaseLifeCostIsOne, Is.True);
            Assert.That(view.CurrentEntry().Experiments.MinionBattleRemovalCapEnabled, Is.False);
            Assert.That(view.CurrentEntry().Experiments.TurnTimer, Is.False);
            view.ShowPage(PrivateLobbyEntryPage.Landing);
            view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(toggle.value, Is.False);
            Assert.That(view.CurrentEntry().Experiments.HeroDefeatBaseLifeCostIsOne, Is.False);
        }
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
        [Test] public void TimerFieldsExposeAllDefaultsAndResetPerGame()
        {
            var view=new PrivateLobbyUnityView(); view.ShowPage(PrivateLobbyEntryPage.Host);
            view.Root.Q<Toggle>("experimental-timer").value=true;
            var durations=new[]{120,30,30,120,300};
            for(int i=0;i<durations.Length;i++)
            {
                var field=view.Root.Q<IntegerField>("experimental-duration-"+i); Assert.That(field.value,Is.EqualTo(durations[i]));
                Assert.That(field.style.color.value, Is.EqualTo(UnityEngine.Color.black), "Values must contrast against the light input background.");
                Assert.That(field.labelElement.style.color.value, Is.EqualTo(UnityEngine.Color.white));
                field.value=durations[i]+5;
            }
            var options=view.CurrentEntry().Experiments; Assert.That(options.TurnTimer,Is.True);Assert.That(options.TeammateCoordination,Is.False);
            Assert.That(options.CardCommitmentSeconds,Is.EqualTo(125));Assert.That(options.TeamBankSeconds,Is.EqualTo(305));
            view.ShowPage(PrivateLobbyEntryPage.Landing);view.ShowPage(PrivateLobbyEntryPage.Host);
            Assert.That(view.CurrentEntry().Experiments.TurnTimer,Is.False);
            Assert.That(view.CurrentEntry().Experiments.CardCommitmentSeconds,Is.EqualTo(120));
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
