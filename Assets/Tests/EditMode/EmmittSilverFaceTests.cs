using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class EmmittSilverFaceTests
    {
        [Test]
        public void UnstableTimeline_IsBasicDefenseOrSkill_WithTwoOrThreeGlitchesAndEnemyChoice()
        {
            var card = EmmittCardContent.UnstableTimelineDefinition;
            Assert.That(card.Initiative.Value, Is.EqualTo(1));
            Assert.That(card.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(card.ActionDefinitions, Has.Count.EqualTo(2));
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var defense = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense);
            Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(4));
            Assert.That(defense.BaseValues.Single(value => value.Channel == PrintedValueChannel.Defense)
                .Amount, Is.EqualTo(6));
            Assert.That(defense.BaseValues.Single(value => value.Channel == PrintedValueChannel.Radius)
                .Amount, Is.EqualTo(4));
            Assert.That(skill.EffectRoot.Nodes.Count(node =>
                node.Capability.Equals(EmmittComponentContent.PlaceGlitchV1)), Is.EqualTo(2));
            Assert.That(defense.EffectRoot.Nodes.Count(node =>
                node.Capability.Equals(EmmittComponentContent.PlaceGlitchV1)), Is.EqualTo(3));
            Assert.That(skill.EffectRoot.Nodes.Any(node =>
                node.Capability.Equals(EmmittGlitchChoiceContent.ChooseGlitchByHeroV1)), Is.True);

            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                EmmittComponentContent.Handlers().Concat(EmmittGlitchChoiceContent.Handlers()));
            Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot, catalog), Is.Empty);
            Assert.That(EffectDefinitionValidator.Validate(defense.EffectRoot, catalog), Is.Empty);
        }
    }
}
