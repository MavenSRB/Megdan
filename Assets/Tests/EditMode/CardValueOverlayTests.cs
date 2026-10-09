using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardValueOverlayTests
    {
        [Test]
        public void EveryPrintedIconOnAllSixHundredFacesHasAnIndividualAnchor()
        {
            var configured = ConfiguredCardArtworkCatalog.ConfiguredReferences
                .Concat(ConfiguredSpellArtwork.ConfiguredReferences).ToArray();
            Assert.That(configured.Length, Is.EqualTo(600));
            var definitions = VerifiedCardContentFaces.ContentClasses().SelectMany(VerifiedCardContentFaces.Of)
                .GroupBy(d => d.Reference).ToDictionary(g => g.Key, g => g.First());
            var missing = new System.Collections.Generic.List<string>();
            foreach (var reference in configured)
            {
                Assert.That(CardValueAnchors.TryGet(reference, out var anchors), Is.True, reference.ToString());
                var face = CurrentRoundCards.ReadFace(definitions[reference]);
                foreach (var value in new[] { face.Initiative, face.PrimaryValue, face.SpatialValue }
                    .Concat(face.SecondaryValues).Where(v => v != null))
                {
                    if (!anchors.TryPosition(face, value.Channel, out var at))
                    {
                        missing.Add(reference + "/" + value.Channel);
                        continue;
                    }
                    Assert.That(at.x, Is.InRange(1f, anchors.Width - 6f), reference.ToString());
                    Assert.That(at.y, Is.InRange(21f, anchors.Height - 21f), reference.ToString());
                }
            }
            // This action's radius-one vocabulary encodes "adjacent" in its text, not a printed icon.
            // The inspected scan deliberately has no numeric spatial anchor to decorate.
            Assert.That(missing, Is.EquivalentTo(new[] {
                "takahide-spinning-blade@takahide-9-32h-rev-1/Radius" }), string.Join("\n", missing));
        }

        [TestCase(-12, "-12")]
        [TestCase(1, "+1")]
        [TestCase(10, "+10")]
        public void ModifiersAreWhiteSignedAndPointerTransparent(int amount, string expected)
        {
            var reference = ConfiguredCardArtworkCatalog.ConfiguredReferences.First();
            var label = CardValueOverlay.BonusLabel(new CardValuePreview(new HeroId("hero"),
                reference, PrintedValueChannel.Defense, amount), 24f);
            Assert.That(label.text, Is.EqualTo(expected));
            Assert.That(label.style.color.value, Is.EqualTo(Color.white));
            Assert.That(label.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(label.style.whiteSpace.value, Is.EqualTo(WhiteSpace.NoWrap));
        }

        [Test]
        public void ModifierWeightUsesWhiteStrokeWithDarkContrast()
        {
            var reference = ConfiguredCardArtworkCatalog.ConfiguredReferences.First();
            var label = CardValueOverlay.BonusLabel(new CardValuePreview(new HeroId("hero"),
                reference, PrintedValueChannel.Attack, 2), 32f);
            Assert.That(label.style.unityTextOutlineColor.value, Is.EqualTo(Color.white));
            Assert.That(label.style.unityTextOutlineWidth.value, Is.GreaterThanOrEqualTo(0.6f));
            Assert.That(label.style.textShadow.value.color, Is.EqualTo(Color.black));
        }

        [Test]
        public void LevelUpCopyRemovesBonusesWithoutChangingPrintedValues()
        {
            var definition = VerifiedCardContentFaces.Of(typeof(GuardsDigital.Core.Cards.Verified.WaspCardContent))
                .First(d => d.Initiative.IsDefined);
            var face = CurrentRoundCards.ReadFace(definition);
            var card = new TrayCardViewModel(new CardInstanceId("card"), definition.Reference,
                definition.DisplayName, definition.Progression.Color, face, TrayCardGroup.Available,
                0, false, false, valueBonuses: new[] { new CardValuePreview(new HeroId("hero"),
                    definition.Reference, PrintedValueChannel.Initiative, 2) });
            Assert.That(card.WithoutValueBonuses().ValueBonuses, Is.Empty);
            Assert.That(card.WithoutValueBonuses().Face, Is.SameAs(face));
            Assert.That(card.ValueBonuses.Count, Is.EqualTo(1));
        }
    }
}
