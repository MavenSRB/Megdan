using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class WidgetDealingTests
    {
        [Test]
        public void NewHandlerTypesExecuteTheSyntheticHazardEscortWithoutPyro()
        {
            var card = GuardsDigital.Core.Cards.Synthetic.SyntheticCoupledEscortHand.Definition;
            var probe = new WidgetTokenOperationTests.Probe(card.ActionDefinitions.Single().EffectRoot,
                new GuardsDigital.Core.Board.HexCoordinate(-3, 0),
                tokenDefinition: GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition);
            var enemy = new GuardsDigital.Core.Units.Heroes.HeroId(StagedBoardParticipants.BlueHeroIdValue);
            probe.Pick(GuardsDigital.Core.Effects.EffectValue.Hero(enemy));
            probe.Pick(GuardsDigital.Core.Effects.EffectValue.Space(new GuardsDigital.Core.Board.HexCoordinate(-1, 0)));
            Assert.That(probe.Choice, Is.Null);
            Assert.That(probe.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(new GuardsDigital.Core.Board.HexCoordinate(-1, 0)));
            Assert.That(probe.Board.PlacedTokens.Single().Definition.Reference,
                Is.EqualTo(GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition.Reference));
            Assert.That(probe.Board.Occupancies.Single(hero => hero.HeroId.Equals(enemy)).Coordinate,
                Is.EqualTo(new GuardsDigital.Core.Board.HexCoordinate(-2, 0)));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void SelectedWidgetCarriesHisEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var widget = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "widget");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(25));
            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { widget }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card => card.OwnerId.Value == widget.HeroIdValue).ToArray();
            Assert.That(cards, Has.Length.EqualTo(18));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(WidgetCardContent.StartingHand));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Deck)
                .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(WidgetCardContent.Deck));
            Assert.That(HeroLineupComposition.Compose(format, lineup).Heroes
                .Single(hero => hero.ContentId.Value == "widget").SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"));
        }

        [TestCase(1, WidgetUpgradeBranch.FirstSibling)]
        [TestCase(2, WidgetUpgradeBranch.FirstSibling)]
        [TestCase(2, WidgetUpgradeBranch.SecondSibling)]
        [TestCase(3, WidgetUpgradeBranch.FirstSibling)]
        [TestCase(3, WidgetUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksUltimate(int tier, WidgetUpgradeBranch branch)
        {
            var configuration = tier == 1 ? MatchConfiguration.CreateStagedWidgetForInternalTest(TeamSide.Blue)
                : tier == 2 ? MatchConfiguration.CreateStagedWidgetTierTwoForInternalTest(TeamSide.Blue, branch)
                : MatchConfiguration.CreateStagedWidgetTierThreeForInternalTest(TeamSide.Blue, branch);
            var ids = WidgetCardContent.DealtDefinitions.Select(definition => definition.Id).ToArray();
            var cards = configuration.CardInstances.Where(card => ids.Contains(card.DefinitionReference.DefinitionId)).ToArray();
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(ids));
            Assert.That(cards.Select(card => card.OwnerId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5));
            var upgraded = tier == 1 ? new[] { WidgetCardContent.DiversionaryStrike, WidgetCardContent.AllAboard, WidgetCardContent.FieryBreath }
                : tier == 2 && branch == WidgetUpgradeBranch.FirstSibling ? new[] { WidgetCardContent.DiversionaryAttack, WidgetCardContent.TakeOff, WidgetCardContent.FlamingBreath }
                : tier == 2 ? new[] { WidgetCardContent.AirborneAttack, WidgetCardContent.DragOff, WidgetCardContent.Nibble }
                : branch == WidgetUpgradeBranch.FirstSibling ? new[] { WidgetCardContent.DiversionaryAssault, WidgetCardContent.SafeLanding, WidgetCardContent.ScorchingBreath }
                : new[] { WidgetCardContent.AirborneAssault, WidgetCardContent.CarryAway, WidgetCardContent.Gnaw };
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand).Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(upgraded.Concat(new[] { WidgetCardContent.FightAsOne, WidgetCardContent.DragonBond })));
            var ultimate = cards.Single(card => card.DefinitionReference.DefinitionId.Equals(WidgetCardContent.DragonKnight));
            Assert.That(ultimate.State.Location, Is.EqualTo(tier == 3 ? CardLocation.UltimateArea : CardLocation.Deck));
        }
    }
}
