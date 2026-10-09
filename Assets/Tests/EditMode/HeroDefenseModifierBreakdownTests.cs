using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroDefenseModifierBreakdownTests
    {
        [Test]
        public void ShiftPanelKeepsEveryCreepSeparateAggregatesLiveItemsAndShowsExactTotal()
        {
            var model = new HeroDefenseModifierProjection(
                new HeroId("hero"),
                1,
                new[]
                {
                    Project(new ValueContribution(ValueContributionKind.MinionDefenseModifier,
                        new MinionId("blue-melee"), PrintedValueChannel.Defense, 1), "Blue Melee creep"),
                    Project(new ValueContribution(ValueContributionKind.MinionDefenseModifier,
                        new MinionId("red-heavy"), PrintedValueChannel.Defense, -1), "Red Heavy creep"),
                    Project(ValueContribution.FromContinuousSource("effective-minion-token:ward",
                        PrintedValueChannel.Defense, 1), "Ward token (creep)"),
                    Project(new ValueContribution(ValueContributionKind.ItemModifier,
                        new CardInstanceId("shield-one"), PrintedValueChannel.Defense, 2), "Shield One"),
                    Project(new ValueContribution(ValueContributionKind.ItemModifier,
                        new CardInstanceId("shield-two"), PrintedValueChannel.Defense, 2), "Shield Two")
                });
            var view = new HeroDefenseModifierBreakdownUnityView();
            var safe = new Rect(200f, 60f, 1300f, 800f);
            var hud = new VisualElement();
            new RuntimeTooltipLayer().Install(hud);
            hud.Add(view.Root);

            view.Show(model, new Vector2(260f, 400f), safe);

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.Root.Query<VisualElement>("gd-hero-defense-creep-row").ToList(),
                Has.Count.EqualTo(3), "each creep or effective creep-like source owns one row");
            Assert.That(view.Root.Query<VisualElement>("gd-hero-defense-items-row").ToList(),
                Has.Count.EqualTo(1), "items collapse only after their live amounts are summed");
            var text = DescendantsAndSelf(view.Root).OfType<Label>().Select(value => value.text).ToArray();
            Assert.That(text, Does.Contain("Blue Melee creep"));
            Assert.That(text, Does.Contain("Red Heavy creep"));
            Assert.That(text, Does.Contain("Ward token (creep)"));
            Assert.That(text, Does.Contain("+4"));
            Assert.That(text, Does.Contain("+5"));
            Assert.That(view.LastRect.xMin,
                Is.GreaterThanOrEqualTo(safe.xMin + HeroDefenseModifierBreakdownUnityView.SafeMargin));
            Assert.That(view.LastRect.xMax,
                Is.LessThanOrEqualTo(safe.xMax - HeroDefenseModifierBreakdownUnityView.SafeMargin));
            Assert.That(DescendantsAndSelf(view.Root).All(value =>
                value.pickingMode == PickingMode.Ignore), Is.True);
        }

        [TestCase(0, 70f)]
        [TestCase(1, 93f)]
        public void PanelHeightIncludesDividerSoTotalStaysInsideBackground(
            int creepCount, float expectedHeight)
        {
            var contributions = Enumerable.Range(0, creepCount)
                .Select(index => Project(new ValueContribution(
                    ValueContributionKind.MinionDefenseModifier,
                    new MinionId("creep-" + index),
                    PrintedValueChannel.Defense,
                    1), "Creep " + index))
                .ToArray();
            var view = new HeroDefenseModifierBreakdownUnityView();

            view.Show(
                new HeroDefenseModifierProjection(new HeroId("hero"), 1, contributions),
                new Vector2(400f, 400f),
                new Rect(0f, 0f, 1200f, 800f));

            Assert.That(view.Root.style.justifyContent.value, Is.EqualTo(Justify.Center),
                "the complete title-to-Total stack is centered, not only the text inside each row");
            Assert.That(view.LastRect.height, Is.EqualTo(expectedHeight),
                "the one-pixel divider and both four-pixel margins must be inside the panel");
            Assert.That(view.Root.Query<VisualElement>("gd-hero-defense-total-divider").ToList(),
                Has.Count.EqualTo(1));
            var title = view.Root.Q<Label>("gd-hero-defense-modifier-title");
            Assert.That(title.style.height.value.value, Is.EqualTo(15f),
                "the title must occupy exactly the height already budgeted by the panel");
            Assert.That(title.style.unityTextAlign.value, Is.EqualTo(TextAnchor.MiddleLeft));
            var total = view.Root.Q<VisualElement>("gd-hero-defense-total-row");
            var labels = total.Children().OfType<Label>().ToArray();
            Assert.That(labels, Has.Length.EqualTo(2));
            Assert.That(labels, Has.All.Matches<Label>(label =>
                label.style.height.value.value == 23f),
                "both text boxes fill the fixed row so their middle alignment has a vertical area");
            Assert.That(labels[0].style.unityTextAlign.value, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(labels[1].style.unityTextAlign.value, Is.EqualTo(TextAnchor.MiddleRight));
        }

        private static ValueContributionProjection Project(ValueContribution value, string label) =>
            new ValueContributionProjection(value, label);

        private static IEnumerable<VisualElement> DescendantsAndSelf(VisualElement root)
        {
            yield return root;
            foreach (var child in root.Children())
                foreach (var descendant in DescendantsAndSelf(child))
                    yield return descendant;
        }
    }
}
