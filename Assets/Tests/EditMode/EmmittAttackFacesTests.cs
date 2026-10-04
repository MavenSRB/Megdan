using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class EmmittAttackFacesTests
    {
        [Test]
        public void ReverseTime_AndTemporalPunchLine_PrintTheirVerifiedValuesAndTiming()
        {
            AssertAttack(EmmittCardContent.ReverseTimeDefinition, 11, 2, 1, 4,
                CardTextTiming.AfterTheAttack, CardTier.None, PrintedValueChannel.None);
            AssertAttack(EmmittCardContent.TemporalPunchDefinition, 7, 7, 3, 9,
                CardTextTiming.BeforeTheAttack, CardTier.I, PrintedValueChannel.None);
            AssertAttack(EmmittCardContent.TemporalSlamDefinition, 7, 7, 3, 11,
                CardTextTiming.BeforeTheAttack, CardTier.II, PrintedValueChannel.Initiative);
            AssertAttack(EmmittCardContent.TemporalJudgementDefinition, 8, 8, 3, 12,
                CardTextTiming.BeforeTheAttack, CardTier.III, PrintedValueChannel.Range);
            AssertAttack(EmmittCardContent.FlashbackDefinition, 7, 6, 3, 5,
                CardTextTiming.AfterTheAttack, CardTier.II, PrintedValueChannel.Defense);
            AssertAttack(EmmittCardContent.DejaVuDefinition, 8, 7, 3, 6,
                CardTextTiming.AfterTheAttack, CardTier.III, PrintedValueChannel.Defense);

            Assert.That(EmmittCardContent.TemporalPunchDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { EmmittCardContent.TemporalSlam, EmmittCardContent.Flashback }));
            Assert.That(EmmittCardContent.TemporalSlamDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { EmmittCardContent.TemporalJudgement, EmmittCardContent.DejaVu }));
        }

        [Test]
        public void FlashbackAndDejaVu_ValidateOptionalPlacementAndEnemyChosenSwap()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                EmmittComponentContent.Handlers().Concat(EmmittGlitchChoiceContent.Handlers()));
            foreach (var (card, count) in new[]
            {
                (EmmittCardContent.FlashbackDefinition, 3),
                (EmmittCardContent.DejaVuDefinition, 2)
            })
            {
                var effect = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack)
                    .EffectRoot;
                Assert.That(EffectDefinitionValidator.Validate(effect, catalog), Is.Empty);
                Assert.That(effect.Nodes.Count(node =>
                    node.Capability.Equals(EmmittComponentContent.PlaceGlitchV1)),
                    Is.EqualTo(count));
                Assert.That(effect.Nodes.Any(node =>
                    node.Capability.Equals(EmmittGlitchChoiceContent.ChooseGlitchByHeroV1)), Is.True);
            }
        }

        [Test]
        public void AlternativeTimelines_RegistersTheSettledStandingRule()
        {
            var card = EmmittCardContent.AlternativeTimelinesDefinition;
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.IV));
            Assert.That(card.ActionDefinitions, Is.Empty);
            Assert.That(card.UltimateRule.RuleKind, Is.EqualTo(TwoCardTurnRule.RuleKind));
        }

        [Test]
        public void Inventory_KeepsIncompleteFrontsOutOfTheDeal_AndBoxesTearInTime()
        {
            Assert.That(EmmittCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(EmmittCardContent.ComposedDefinitions.Count, Is.EqualTo(19));
            Assert.That(EmmittCardContent.NotYetComposed, Is.Empty);
            Assert.That(EmmittCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(EmmittCardContent.StartingHand,
                Is.EquivalentTo(new[] { EmmittCardContent.ReverseTime,
                    EmmittCardContent.UnstableTimeline, EmmittCardContent.TemporalPunch,
                    EmmittCardContent.TimeSnare, EmmittCardContent.TimeWalk }));
            Assert.That(EmmittCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { EmmittCardContent.TearInTime }));
            Assert.That(EmmittCardContent.Deck.Count, Is.EqualTo(13));
            var handicap = EmmittCardContent.TearInTimeDefinition;
            AssertAttack(handicap, 10, 1, 1, 3, CardTextTiming.AfterTheAttack,
                CardTier.None, PrintedValueChannel.None);
            Assert.That(handicap.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack)
                .AttackTargetPolicy.Value.TargetableUnits, Is.EqualTo(AttackTargetableUnits.HeroesOnly));
        }

        private static void AssertAttack(CardDefinition card, int initiative, int defense,
            int movement, int attack, CardTextTiming timing, CardTier tier,
            PrintedValueChannel item)
        {
            var primary = card.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.Progression.Tier, Is.EqualTo(tier));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item));
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(primary.BaseValues.Single(value => value.Channel == PrintedValueChannel.Attack).Amount,
                Is.EqualTo(attack));
            Assert.That(primary.CardTextTiming, Is.EqualTo(timing));
            Assert.That(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount, Is.EqualTo(defense));
            Assert.That(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                .BaseValues.Single().Amount, Is.EqualTo(movement));
        }
    }
}
