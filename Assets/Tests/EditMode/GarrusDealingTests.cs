using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class GarrusDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedGarrusCarriesHisEntireSetIntoEitherOfficialFormat(int perTeam)
        {
            var garrus = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "garrus");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(32));
            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { garrus }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card => card.OwnerId.Value == garrus.HeroIdValue).ToArray();
            Assert.That(cards, Has.Length.EqualTo(18));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(GarrusCardContent.StartingHand));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Deck)
                .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(GarrusCardContent.Deck));
            Assert.That(HeroLineupComposition.Compose(format, lineup).Heroes
                .Single(hero => hero.ContentId.Value == "garrus").SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"));
        }

        [TestCase(1, GarrusUpgradeBranch.FirstSibling)]
        [TestCase(2, GarrusUpgradeBranch.FirstSibling)]
        [TestCase(2, GarrusUpgradeBranch.SecondSibling)]
        [TestCase(3, GarrusUpgradeBranch.FirstSibling)]
        [TestCase(3, GarrusUpgradeBranch.SecondSibling)]
        public void StagedConfigurationsDealEveryUpgradeAndOnlyTierThreeUnlocksUltimate(int tier, GarrusUpgradeBranch branch)
        {
            var configuration = tier == 1 ? MatchConfiguration.CreateStagedGarrusForInternalTest(TeamSide.Blue)
                : tier == 2 ? MatchConfiguration.CreateStagedGarrusTierTwoForInternalTest(TeamSide.Blue, branch)
                : MatchConfiguration.CreateStagedGarrusTierThreeForInternalTest(TeamSide.Blue, branch);
            var ids = GarrusCardContent.DealtDefinitions.Select(definition => definition.Id).ToArray();
            var cards = configuration.CardInstances.Where(card => ids.Contains(card.DefinitionReference.DefinitionId)).ToArray();
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(ids));
            Assert.That(cards.Select(card => card.OwnerId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5));
            var upgraded = tier == 1 ? new[] { GarrusCardContent.Trace, GarrusCardContent.Menace, GarrusCardContent.HoldGround }
                : tier == 2 && branch == GarrusUpgradeBranch.FirstSibling ? new[] { GarrusCardContent.Chase, GarrusCardContent.Threaten, GarrusCardContent.MakeAStand }
                : tier == 2 ? new[] { GarrusCardContent.BluntForce, GarrusCardContent.FormUp, GarrusCardContent.LightPilum }
                : branch == GarrusUpgradeBranch.FirstSibling ? new[] { GarrusCardContent.HuntDown, GarrusCardContent.Terrify, GarrusCardContent.BattleReady }
                : new[] { GarrusCardContent.SendFlying, GarrusCardContent.Testudo, GarrusCardContent.HeavyPilum };
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand).Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(upgraded.Concat(new[] { GarrusCardContent.AngryStrike, GarrusCardContent.ChillingHowl })));
            var ultimate = cards.Single(card => card.DefinitionReference.DefinitionId.Equals(GarrusCardContent.BattleFury));
            Assert.That(ultimate.State.Location, Is.EqualTo(tier == 3 ? CardLocation.UltimateArea : CardLocation.Deck));
        }
    }
}
