using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class HeroTrayRefinementTests
    {
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
