using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z58. User directive (2026-09-07): "when leveling. Show current
    /// hand (all 5 cards) above the offer." A seat deciding rulebook p.15's Upgrade a Card sees a
    /// read-only reminder of what it currently holds, drawn above the upgrade comparison band,
    /// sourced from the same seat-scoped projection the ordinary hand tray reads (never a second,
    /// widened derivation).
    /// </summary>
    public sealed class LevelUpHandPreviewTests
    {
        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                yield return child;
                foreach (var grandchild in Descendants(child))
                {
                    yield return grandchild;
                }
            }
        }

        private static (RoundActionViewModel Model, RoundActionUnityView View, HeroId HeroId)
            RenderLevelingHero()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(
                out var heroId);
            var model = RoundActionPresenter.CreateViewModel(session.Projection, heroId);
            var view = new RoundActionUnityView();
            view.Render(model);
            return (model, view, heroId);
        }

        [Test]
        public void LevelUpHandPreview_ShowsAboveTheOfferWhenTheWindowOpens()
        {
            var (model, view, _) = RenderLevelingHero();

            Assert.That(model.SystemTransitionPrompt, Is.Not.Null,
                "Sanity: the fixture actually reaches p.15's Upgrade a Card pause.");
            Assert.That(model.LevelUpHandPreview, Is.Not.Empty,
                "Sanity: the levelling hero's own hand is not empty at this fixture's state.");

            var all = Descendants(view.Root).ToList();
            var previewRow = all.SingleOrDefault(element => element.name == "gd-levelup-hand-preview");
            var offerBand = all.SingleOrDefault(
                element => element.name == "gd-upgrade-comparison-band");
            Assert.That(previewRow, Is.Not.Null, "The hand preview renders while the window is open.");
            Assert.That(offerBand, Is.Not.Null, "Sanity: the upgrade offer itself renders too.");
            Assert.That(all.IndexOf(previewRow), Is.LessThan(all.IndexOf(offerBand)),
                "\"Above the offer\": the preview must appear before the comparison band in " +
                "document order, which is also paint and reading order in this vertical stack.");
        }

        [Test]
        public void LevelUpHandPreview_ShowsExactlyTheSeatsOwnCardsWithNoInventedPadding()
        {
            var (model, view, heroId) = RenderLevelingHero();

            var actualHand = model.LevelUpHandPreview
                .Select(card => card.CardInstanceId)
                .OrderBy(id => id.Value)
                .ToArray();
            // Independently derived, not read back from the model under test: the seat's own
            // projection, filtered the same way the ordinary hand tray already filters it
            // (OwnerId + Location == Hand) - two readings of the same source rather than one
            // circular check.
            var projection = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out var again)
                .Projection;
            Assert.That(again, Is.EqualTo(heroId),
                "Sanity: the fixture deals the same levelling hero every call.");
            var expectedHand = projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.Id)
                .OrderBy(id => id.Value)
                .ToArray();

            Assert.That(actualHand, Is.EqualTo(expectedHand),
                "Exactly the seat's own current hand - never a padded five, never another hero's.");

            var cardElements = Descendants(view.Root)
                .Where(element => element.name == "gd-levelup-hand-preview-card")
                .ToArray();
            Assert.That(cardElements.Length, Is.EqualTo(expectedHand.Length),
                "One preview slot per hand card, whatever the count actually is.");
        }

        [Test]
        public void LevelUpHandPreview_IsReadOnlyAndNeverInterceptsThePointer()
        {
            var (_, view, _) = RenderLevelingHero();

            var previewRow = Descendants(view.Root)
                .Single(element => element.name == "gd-levelup-hand-preview");
            Assert.That(previewRow.pickingMode, Is.EqualTo(PickingMode.Ignore));

            var slots = Descendants(previewRow).Where(
                element => element.name == "gd-levelup-hand-preview-card").ToArray();
            Assert.That(slots, Is.Not.Empty);
            foreach (var slot in slots)
            {
                Assert.That(slot.pickingMode, Is.EqualTo(PickingMode.Ignore));
                foreach (var descendant in Descendants(slot))
                {
                    Assert.That(descendant.pickingMode, Is.EqualTo(PickingMode.Ignore),
                        "Every element under a preview slot ignores the pointer, so it can never " +
                        "compete with the offer's own buttons for a click.");
                    Assert.That(descendant, Is.Not.InstanceOf<Button>(),
                        "The preview draws no clickable control at all - read-only by construction, " +
                        "not merely by omitting a handler.");
                }
            }
        }

        [Test]
        public void LevelUpHandPreview_IsEmptyOutsideTheLevelingWindow()
        {
            var model = new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                System.Array.Empty<TypedInvocationViewModel>(), null, null,
                System.Array.Empty<TypedInvocationViewModel>(),
                System.Array.Empty<TypedInvocationViewModel>());
            var view = new RoundActionUnityView();

            view.Render(model);

            Assert.That(model.LevelUpHandPreview, Is.Empty);
            Assert.That(
                Descendants(view.Root).Any(element => element.name == "gd-levelup-hand-preview"),
                Is.False);
        }
    }
}
