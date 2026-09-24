using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsRedCompositionTests
    {
        [Test]
        public void FiveRedFaces_KeepTheirPrintedValuesAndProgression()
        {
            var reds = TrinketsCardContent.RedDefinitions;
            Assert.That(reds, Has.Count.EqualTo(5));
            Assert.That(reds.Select(card => card.Id).Distinct().Count(), Is.EqualTo(5));
            var expected = new[]
            {
                (8, 5, 4, 3, 2), (9, 6, 4, 3, 3), (9, 6, 4, 4, 4),
                (9, 6, 4, 4, 3), (9, 6, 4, 5, 4)
            };
            var tiers = new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III };
            var items = new[]
            {
                PrintedValueChannel.None, PrintedValueChannel.Defense,
                PrintedValueChannel.Initiative, PrintedValueChannel.Defense,
                PrintedValueChannel.Radius
            };
            for (var index = 0; index < reds.Count; index++)
            {
                var card = reds[index];
                var attack = card.ActionDefinitions.Single(action =>
                    action.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
                var movement = card.ActionDefinitions.Single(action =>
                    action.Id.Equals(TrinketsCardContent.SecondaryMovement));
                var defense = card.ActionDefinitions.Single(action =>
                    action.Id.Equals(TrinketsCardContent.SecondaryDefense));
                Assert.That(card.Initiative.Value, Is.EqualTo(expected[index].Item1), card.DisplayName);
                Assert.That(card.Progression.Tier, Is.EqualTo(tiers[index]), card.DisplayName);
                Assert.That(card.Progression.ItemIcon, Is.EqualTo(items[index]), card.DisplayName);
                Assert.That(defense.BaseValues.Single().Amount, Is.EqualTo(expected[index].Item2), card.DisplayName);
                Assert.That(movement.BaseValues.Single().Amount, Is.EqualTo(expected[index].Item3), card.DisplayName);
                Assert.That(attack.BaseValues.Single(value =>
                    value.Channel == PrintedValueChannel.Attack).Amount,
                    Is.EqualTo(expected[index].Item4), card.DisplayName);
                Assert.That(attack.BaseValues.Single(value =>
                    value.Channel == PrintedValueChannel.Range).Amount,
                    Is.EqualTo(expected[index].Item5), card.DisplayName);
                Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack));
                Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            }

            Assert.That(reds[0].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { reds[1].Id, reds[2].Id }));
            Assert.That(reds[1].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { reds[3].Id, reds[4].Id }));
            Assert.That(reds[2].Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { reds[3].Id, reds[4].Id }));
            Assert.That(reds[3].Progression.NextTierCandidateIds, Is.Empty);
            Assert.That(reds[4].Progression.NextTierCandidateIds, Is.Empty);
        }

        [Test]
        public void AllRedAttackBodies_AreLegalWithContentOwnedObstacleHandlers()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers());
            foreach (var card in TrinketsCardContent.RedDefinitions)
            {
                var attack = card.ActionDefinitions.Single(action =>
                    action.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
                var errors = EffectDefinitionValidator.ValidateCardText(
                    attack.EffectRoot, catalog, attack.CardTextTiming);
                Assert.That(errors, Is.Empty,
                    card.DisplayName + ": " + string.Join("; ", errors.Select(error => error.ToString())));
                if (attack.SecondCardTextRoot != null)
                {
                    var afterErrors = EffectDefinitionValidator.ValidateCardText(
                        attack.SecondCardTextRoot, catalog, attack.SecondCardTextTiming);
                    Assert.That(afterErrors, Is.Empty,
                        card.DisplayName + " after attack: " +
                        string.Join("; ", afterErrors.Select(error => error.ToString())));
                }
            }
        }

        [Test]
        public void SteamAndFlame_DeclareOptionalDifferentTargetRepeatsAtTheirPrintedCounts()
        {
            var reds = TrinketsCardContent.RedDefinitions;
            for (var index = 0; index < reds.Count; index++)
            {
                var attack = reds[index].ActionDefinitions.Single(action =>
                    action.Id.Equals(TrinketsCardContent.PrimaryRangedAttack));
                var repeats = (attack.SecondCardTextRoot?.Nodes ??
                    Array.Empty<EffectNodeDefinition>()).Where(node =>
                    node.Kind == EffectNodeKind.RepeatAction).ToArray();
                if (index != 2 && index != 4)
                {
                    Assert.That(repeats, Is.Empty, reds[index].DisplayName);
                    continue;
                }

                Assert.That(repeats, Has.Length.EqualTo(1), reds[index].DisplayName);
                Assert.That(repeats[0].FailurePolicy, Is.EqualTo(EffectFailurePolicy.May));
                Assert.That(repeats[0].Chooser, Is.EqualTo(EffectChooserRole.Actor));
                Assert.That(repeats[0].MaximumRepeats, Is.EqualTo(index == 2 ? 1 : 2));
                Assert.That(repeats[0].RepeatTargetBinding,
                    Is.EqualTo(RepeatTargetBinding.DifferentTarget));
            }
        }

        [Test]
        public void DualLineContribution_RequiresAnAttackChannelAndTrustedObstacleKind()
        {
            var query = RosterValueQueryKind.AttackTargetInStraightLineFromActorAndOwnedObstacle;
            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack, query, 2),
                Throws.ArgumentException);
            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Defense, query, 2,
                    TrinketsComponentContent.TurretKind),
                Throws.ArgumentException);
            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack,
                    RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                    2, TrinketsComponentContent.TurretKind),
                Throws.ArgumentException);
        }
    }
}
