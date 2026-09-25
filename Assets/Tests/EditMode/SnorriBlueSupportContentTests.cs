using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueSupportContentTests
    {
        [Test]
        public void BothAncestralFacesHavePrintedSkillMovementDefenseAndItem()
        {
            Assert.That(SnorriBlueSupportFaces.Definitions.Select(face =>
                face.Initiative.Value), Is.EqualTo(new[] { 9, 10 }));
            foreach (var face in SnorriBlueSupportFaces.Definitions)
            {
                Assert.That(face.Progression.ItemIcon,
                    Is.EqualTo(PrintedValueChannel.Defense));
                Assert.That(face.ActionDefinitions.Single(action =>
                    action.Kind == ActionKind.Skill).BaseValues.Single().Amount, Is.EqualTo(4));
                Assert.That(face.ActionDefinitions.Single(action =>
                    action.Kind == ActionKind.Movement).BaseValues.Single().Amount, Is.EqualTo(3));
                Assert.That(face.ActionDefinitions.Single(action =>
                    action.Kind == ActionKind.Defense).BaseValues.Single().Amount, Is.EqualTo(6));
            }
        }

        [Test]
        public void AncestralGraphsUseRegisteredTypedChoices()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                SnorriComponentContent.Handlers()
                    .Concat(SnorriBlueSupportCapabilities.Handlers()));
            foreach (var face in SnorriBlueSupportFaces.Definitions)
            {
                var skill = face.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Skill);
                var errors = EffectDefinitionValidator.ValidateCardText(
                    skill.EffectRoot, catalog, skill.CardTextTiming);
                Assert.That(errors, Is.Empty, face.DisplayName + ": " +
                    string.Join("; ", errors));
                Assert.That(skill.EffectRoot.Nodes.Count(node =>
                    node.Kind == EffectNodeKind.Optional &&
                    node.Chooser == EffectChooserRole.BoundHero),
                    Is.EqualTo(face.Id.Equals(SnorriBlueMovementFaces.AncestralGrace)
                        ? 3 : 2));
            }
        }
    }
}
