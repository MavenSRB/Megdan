using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Milestone 9.49: Emmitt's complete, selectable printed set.</summary>
    public sealed class EmmittDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedEmmitt_DealsExactlyEighteenFronts_AndBoxesTheHandicap(int perTeam)
        {
            var emmitt = RegisteredHeroRoster.ForThisBuild.Single(hero =>
                hero.HeroIdValue == QuickGameSetupConfiguration.EmmittHeroIdValue);
            Assert.That(emmitt.DisplayName, Is.EqualTo("Emmitt the Traveller"));
            Assert.That(emmitt.Complexity, Is.EqualTo(HeroComplexity.Wizard));
            Assert.That(emmitt.Roles, Is.EqualTo(HeroRole.Melee | HeroRole.Tactician |
                HeroRole.Durable | HeroRole.Disabler | HeroRole.Tokens));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { emmitt }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == emmitt.HeroIdValue).ToArray();

            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference),
                Is.EquivalentTo(EmmittCardContent.DealtDefinitions.Select(card => card.Reference)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(EmmittCardContent.TearInTime));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(EmmittCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(13));
        }

        [Test]
        public void EveryFrontIsComposed_AndEveryEffectUsesTheProductionCatalog()
        {
            Assert.That(EmmittCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(EmmittCardContent.NotYetComposed, Is.Empty);
            Assert.That(EmmittCardContent.AllDefinitions.Select(card => card.Id),
                Is.EqualTo(EmmittCardContent.InventoryOrder));
            Assert.That(EmmittCardContent.AllDefinitions.Select(card => card.Reference.ContentRevision),
                Is.All.EqualTo(EmmittCardContent.ContentRevision));
            Assert.That(EmmittCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(EmmittCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { EmmittCardContent.TearInTime }));

            var catalog = MatchContentCatalogs.ForThisBuild();
            foreach (var card in EmmittCardContent.DealtDefinitions)
                foreach (var action in card.ActionDefinitions.Where(action => action.EffectRoot != null))
                    Assert.That(GuardsDigital.Core.Effects.EffectDefinitionValidator.Validate(
                        action.EffectRoot, catalog.Capabilities), Is.Empty, card.Id.Value);
        }
    }
}
