using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroCardPipPreviewTests
    {
        [Test]
        public void CompleteFaceStaysInsideTheBoardSafeRegionAndNeverBlocksPointerInput()
        {
            using (ConfiguredCardArtwork.UseTextureLoader(_ => null))
            {
                var view = new HeroCardPipPreviewUnityView();
                var safe = new Rect(260f, 72f, 1900f, 980f);
                var first = WaspCardContent.Definition(WaspCardContent.Shock);
                var second = WaspCardContent.Definition(WaspCardContent.MagneticDagger);

                view.Show(first, new Vector2(safe.xMin + 40f, safe.center.y), safe);

                Assert.That(view.IsVisible, Is.True);
                Assert.That(view.DefinitionReference, Is.EqualTo(first.Reference));
                Assert.That(view.LastRect.width, Is.EqualTo(CardFootprint.Width));
                Assert.That(view.LastRect.height, Is.EqualTo(CardFootprint.Height));
                Assert.That(view.LastRect.xMin,
                    Is.GreaterThanOrEqualTo(safe.xMin + HeroCardPipPreviewUnityView.SafeMargin));
                Assert.That(view.LastRect.xMax,
                    Is.LessThanOrEqualTo(safe.xMax - HeroCardPipPreviewUnityView.SafeMargin));
                Assert.That(view.LastRect.yMin,
                    Is.GreaterThanOrEqualTo(safe.yMin + HeroCardPipPreviewUnityView.SafeMargin));
                Assert.That(view.LastRect.yMax,
                    Is.LessThanOrEqualTo(safe.yMax - HeroCardPipPreviewUnityView.SafeMargin));
                Assert.That(DescendantsAndSelf(view.Root).All(element =>
                    element.pickingMode == PickingMode.Ignore), Is.True,
                    "the preview and every complete-face child remain transparent to board input");
                Assert.That(DescendantsAndSelf(view.Root).Single(element =>
                    element.name == "gd-hero-token-card-pip-face").style.width.value.value,
                    Is.EqualTo(CardFootprint.Width));

                var rightPointer = new Vector2(safe.xMax - 40f, safe.center.y);
                view.Show(second, rightPointer, safe);

                Assert.That(view.DefinitionReference, Is.EqualTo(second.Reference),
                    "moving directly to a different pip replaces the face");
                Assert.That(view.LastRect.xMax, Is.EqualTo(rightPointer.x - HeroCardPipPreviewUnityView.PointerGap).Within(0.01f),
                    "a right-side pip opens inward, away from the right-side controls");

                view.Hide();
                Assert.That(view.IsVisible, Is.False);
                Assert.That(view.Root.childCount, Is.Zero);
            }
        }

        [Test]
        public void SpellColumnIsBesideTheFaceBoundedAndRefreshesRemovedSpells()
        {
            using (ConfiguredCardArtwork.UseTextureLoader(_ => null))
            {
                var view = new HeroCardPipPreviewUnityView();
                var card = GydionCardContent.Definition(GydionCardContent.Cantrip);
                var spells = new[] { GydionCardContent.FindFamiliar, GydionCardContent.Fireball,
                    GydionCardContent.Sunburst }.Select(id =>
                        new SpellCardViewModel(GydionCardContent.Definition(id))).ToArray();
                var safe = new Rect(260, 72, 1400, 800);
                foreach (var pointer in new[] { safe.min, safe.max })
                {
                    view.Show(card, pointer, safe, spells);
                    var shelf = view.Root.Q("gd-castable-spells");
                    Assert.That(shelf.style.left.value.value, Is.GreaterThan(CardFootprint.Width));
                    Assert.That(shelf.style.top.value.value, Is.Zero);
                    Assert.That(view.Root.Query("gd-spell-face").ToList(), Has.Count.EqualTo(3));
                    Assert.That(view.LastRect.xMin, Is.GreaterThanOrEqualTo(safe.xMin));
                    Assert.That(view.LastRect.yMin, Is.GreaterThanOrEqualTo(safe.yMin));
                    Assert.That(view.LastRect.xMax, Is.LessThanOrEqualTo(safe.xMax));
                    Assert.That(view.LastRect.yMax, Is.LessThanOrEqualTo(safe.yMax));
                }
                var retained = view.Root[0];
                view.Show(card, safe.center, safe, spells.ToArray());
                Assert.That(view.Root[0], Is.SameAs(retained));
                spells[1] = new SpellCardViewModel(GydionCardContent.Definition(GydionCardContent.Fireball), true);
                view.Show(card, safe.center, safe, spells);
                Assert.That(view.Root.Query("gd-spell-greyed").ToList(), Has.Count.EqualTo(1));
                Assert.That(DescendantsAndSelf(view.Root).All(e => e.pickingMode == PickingMode.Ignore), Is.True);
                view.Show(WaspCardContent.Definition(WaspCardContent.Shock), safe.center, safe);
                Assert.That(view.Root.Q("gd-castable-spells"), Is.Null);
                view.Hide();
                Assert.That(view.IsVisible, Is.False);
            }
        }

        private static IEnumerable<VisualElement> DescendantsAndSelf(VisualElement root)
        {
            yield return root;
            foreach (var child in root.Children())
            {
                foreach (var descendant in DescendantsAndSelf(child))
                    yield return descendant;
            }
        }
    }
}
