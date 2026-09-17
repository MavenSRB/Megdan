using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class HeroTrayRefinementTests
    {
        [UnityTest]
        public IEnumerator PlayerTokenAndPlaqueClicksKeepOwnCardsPlayable()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var seat = Root().Runtime.Host.Seats.Seats[1];
            MatchSceneCompositionRoot.NextComposedSeat = seat.Id;
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = Root();
            var runtime = root.Runtime;
            Assert.That(runtime.Seat.Id, Is.EqualTo(seat.Id));
            var pointer = UnityEngine.Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var version = runtime.ReadModel.Version;
            foreach (var other in runtime.Setup.Heroes.Where(hero => !hero.HeroId.Equals(seat.HeroId)))
            {
                UiPlayer.ClickBoard(pointer, root.Hud, HeroView(other.HeroId).transform.position);
                yield return null;
                UiPlayer.Click(Rail(root, other.HeroId).Q("gd-hero-identity-plate"));
                yield return null;
                Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId));
                Assert.That(runtime.HandsView.Model.ViewedHand.HeroId, Is.EqualTo(seat.HeroId));
                Assert.That(runtime.HandsView.Model.Hands.Select(hand => hand.HeroId),
                    Is.EqualTo(new[] { seat.HeroId }));
            }
            UiPlayer.Click(Rail(root, seat.HeroId).Q("gd-hero-identity-plate"));
            yield return null;
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            var ownCard = UiPlayer.Descendants(runtime.HandsView.Root).OfType<Button>()
                .Last(button => button.name == "gd-card-choice" && button.enabledInHierarchy && UiPlayer.IsShown(button));
            UiPlayer.Click(ownCard);
            yield return null;
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Not.Null);
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONFIRM FACEDOWN"));
            yield return null;
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version + 1));
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId));
        }

        [UnityTest]
        public IEnumerator DirectorPlaqueClickSelectsHeroWithoutChangingTheMatch()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return Stage(GarrusCardContent.AngryStrike);
            var root = Root();
            var runtime = root.Runtime;
            var other = runtime.Setup.Heroes.First(hero => !hero.HeroId.Equals(runtime.ViewedHeroId));
            var version = runtime.ReadModel.Version;
            UiPlayer.PointAt(Rail(root, other.HeroId).Q("gd-hero-medallion"));
            yield return null;
            Assert.That(UiPlayer.IsShown(runtime.HeroOverviewsView.HoverPreviewRoot), Is.True);
            UiPlayer.Click(Rail(root, other.HeroId).Q("gd-hero-identity-plate"));
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(other.HeroId));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(other.HeroId)).IsViewed,
                Is.True);
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            Assert.That(UiPlayer.IsShown(runtime.HeroOverviewsView.HoverPreviewRoot), Is.False,
                "Rebuilding the viewed-hero model drops its old preview.");
        }

        [UnityTest]
        public IEnumerator PortraitHoverShowsMatchingPickCardInsideBothViewportsAndDismisses()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            // A normal match, not staging: the sandbox-only banner covers Red's portrait at 540px.
            var roster = RegisteredHeroRoster.ForThisBuild;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { "wasp", "xargatha", "arien" }.Select(id => roster.Single(hero => hero.ContentId.Value == id)),
                new[] { "garrus", "sabina", "tigerclaw" }.Select(id => roster.Single(hero => hero.ContentId.Value == id)));
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = Root();
            var runtime = root.Runtime;
            var version = runtime.ReadModel.Version;
            foreach (var height in new uint[] { 1080, 540 })
            {
                UiPlayer.SetPlayerViewport(height == 540 ? 2560u : 1920u, height, "hero pick-card hover");
                yield return null; yield return null;
                root.Hud.Layout();
                yield return null;
                foreach (var hero in runtime.HeroOverviewsView.Model.Heroes)
                {
                    var portrait = Rail(root, hero.HeroId).Q("gd-hero-medallion");
                    UiPlayer.PointAt(portrait);
                    yield return null;
                    var preview = runtime.HeroOverviewsView.HoverPreviewRoot;
                    var face = preview.Q<Image>("gd-hero-profile-preview");
                    Assert.That(face, Is.Not.Null);
                    var registered = RegisteredHeroRoster.ForThisBuild.Single(value => value.ContentId.Equals(hero.ContentId));
                    Assert.That(face.image, Is.SameAs(Resources.Load<Texture2D>("Private/HeroSelection/" + registered.HeroIdValue)));
                    Assert.That(face.image, Is.Not.Null);
                    Assert.That(UiPlayer.IsShown(face), Is.True);
                    Assert.That(preview.childCount, Is.EqualTo(1));
                    Assert.That(face.pickingMode, Is.EqualTo(PickingMode.Ignore));
                    var bounds = root.Hud.RootElement.worldBound;
                    Assert.That(face.worldBound.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 1));
                    Assert.That(face.worldBound.xMax, Is.LessThanOrEqualTo(bounds.xMax + 1));
                    Assert.That(face.worldBound.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 1));
                    Assert.That(face.worldBound.yMax, Is.LessThanOrEqualTo(bounds.yMax + 1));
                    if (hero.ContentId.Value == "garrus" || hero.ContentId.Value == "wasp")
                        yield return UiPlayer.CaptureGameView("8-42b", "profile-" + registered.HeroIdValue + "-" + height);
                    UiPlayer.PointAway(portrait);
                    yield return null;
                    Assert.That(UiPlayer.IsShown(preview), Is.False);
                    Assert.That(preview.childCount, Is.Zero);
                }
            }
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            UiPlayer.SetPlayerViewport(1920, 1080, "restore profile viewport");
        }

        private static VisualElement Rail(MatchSceneCompositionRoot root, HeroId hero) =>
            UiPlayer.Descendants(root.Hud.RootElement).Single(element => element.name == "gd-hero-rail" &&
                element.userData is HeroId id && id.Equals(hero));

        [UnityTest]
        public IEnumerator InitiativeBadgeShowsEarnedBonusAndKeepsItsFootprint()
        {
            yield return Stage(GarrusCardContent.AngryStrike, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var owner = Garrus(runtime);
            for (var step = 0; step < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(owner)); step++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var option = runtime.RoundView.Model.SystemTransitionPrompt.Options.First(value =>
                value.Card != null && value.ItemIcon == PrintedValueChannel.Initiative);
            var button = UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip == option.Label + " - " + option.Detail);
            UiPlayer.EnsureVisible(button);
            yield return null;
            UiPlayer.Click(button);
            yield return null;
            for (var step = 0; step < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; step++)
                yield return NextRoundDecision(root);
            var bonus = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(owner))
                .ItemBonuses.Single(value => value.Key == PrintedValueChannel.Initiative).Value;
            Assert.That(bonus, Is.GreaterThan(0));
            var held = runtime.Cards.First(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);
            yield return CommitEverySeat(root, owner, NameOf(runtime, held.DefinitionReference.DefinitionId));
            yield return new UnityEngine.WaitForSeconds(0.5f);

            foreach (var height in new uint[] { 1080, 540 })
            {
                UiPlayer.SetPlayerViewport(height == 540 ? 2560u : 1920u, height, "Initiative badge");
                yield return null; yield return null;
                root.Hud.Layout();
                yield return null;
                var model = runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(owner));
                Assert.That(model.CurrentCard, Is.Not.Null);
                var expected = (model.CurrentCard.Card.Face.Initiative.Amount + bonus).ToString();
                var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(element =>
                    element.name == "gd-hero-rail" && element.userData is HeroId id && id.Equals(owner));
                var marker = rail.Q<VisualElement>("gd-current-card");
                var label = UiPlayer.Descendants(marker).OfType<Label>().First();
                Assert.That(label.text, Is.EqualTo(expected));
                Assert.That(UiPlayer.IsShown(label), Is.True);
                Assert.That(label.worldBound.xMin, Is.GreaterThanOrEqualTo(marker.worldBound.xMin - 1));
                Assert.That(label.worldBound.xMax, Is.LessThanOrEqualTo(marker.worldBound.xMax + 1));
                yield return UiPlayer.CaptureGameView("8-42a", "initiative-" + height);
            }
            UiPlayer.SetPlayerViewport(1920, 1080, "restore Initiative viewport");
            Healthy(runtime);
        }
    }
}
