using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class EmmittBlueFacesTests
    {
        [Test]
        public void SnareAndTrap_PrintTheirExactFronts_AndValidateTheirResolvedHeroSelector()
        {
            AssertFace(EmmittCardContent.TimeSnareDefinition, 8, 3, 2,
                CardTier.I, PrintedValueChannel.None);
            AssertFace(EmmittCardContent.TimeTrapDefinition, 9, 4, 3,
                CardTier.II, PrintedValueChannel.Defense);
            AssertFace(EmmittCardContent.TimeLoopDefinition, 9, 4, 4,
                CardTier.II, PrintedValueChannel.Attack);
            AssertFace(EmmittCardContent.TimeBombDefinition, 9, 4, 3,
                CardTier.III, PrintedValueChannel.Movement);
            Assert.That(EmmittCardContent.TimeSnareDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { EmmittCardContent.TimeTrap, EmmittCardContent.TimeLoop }));
            Assert.That(EmmittCardContent.TimeTrapDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { EmmittCardContent.TimeBomb, EmmittCardContent.TimeWarp }));
        }

        [Test]
        public void TimeWarp_IsAnExactChooseOne_WithEnemyOwnedResolvedCardChoice()
        {
            var card = EmmittCardContent.TimeWarpDefinition;
            Assert.That(card.Initiative.Value, Is.EqualTo(9));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Radius));
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(4));
            Assert.That(skill.EffectRoot.Nodes.Single(node => node.Id.Equals(skill.EffectRoot.RootNodeId))
                .Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(skill.EffectRoot.Nodes.Any(node => node.Capability.Equals(
                EmmittTimeWarpContent.SwapWithResolvedV1)), Is.True);
            Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities), Is.Empty);
        }

        private static void AssertFace(CardDefinition card, int initiative, int defense,
            int range, CardTier tier, PrintedValueChannel item)
        {
            var primary = card.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.Progression.Tier, Is.EqualTo(tier));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item));
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(primary.BaseValues.Single().Amount, Is.EqualTo(range));
            Assert.That(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount, Is.EqualTo(defense));
            Assert.That(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                .BaseValues.Single().Amount, Is.EqualTo(2));
            Assert.That(EffectDefinitionValidator.Validate(primary.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities), Is.Empty);
            Assert.That(primary.EffectRoot.Nodes.Any(node =>
                node.Capability.Equals(SyntheticTargetingCapabilities.RangeHeroTargetV1) &&
                node.Inputs.Any(input => input.ParameterName ==
                    SyntheticTargetingCapabilities.ResolvedCardThisTurnParameterName)), Is.True);
        }
    }
}
