using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8f: Mortimer the Awakener dealt into both formats, with his Zombie token and his
    /// handlers registered, the printed three-star, five-role picker row, and his prepared private art.
    /// </summary>
    public sealed class MortimerDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedMortimer_DealsTheEighteenPlayableFacesInEitherFormat(int perTeam)
        {
            var mortimer = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "mortimer");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(27));
            Assert.That(mortimer.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.MortimerHeroIdValue));
            Assert.That(mortimer.DisplayName, Is.EqualTo("Mortimer the Awakener"));
            Assert.That(mortimer.Complexity, Is.EqualTo(HeroComplexity.VeryComplex), "three stars");
            Assert.That(mortimer.Roles, Is.EqualTo(HeroRole.Melee | HeroRole.Tokens | HeroRole.Farming |
                HeroRole.Durable | HeroRole.Pusher));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { mortimer }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == mortimer.HeroIdValue).ToArray();
            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MortimerCardContent.DealtDefinitions.Select(card => card.Id)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(MortimerCardContent.ShallowGraves),
                "Shallow Graves is the p.20 Handicap and returns to the box at setup.");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MortimerCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(13));
        }

        [Test]
        public void TheTrustedCatalog_RegistersTheZombieAndHisHandlers()
        {
            var staged = MatchConfiguration.CreateStagedMortimerForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("mortimer-", StringComparison.Ordinal)), Is.EqualTo(18));

            var catalogs = MatchContentCatalogs.ForThisBuild();
            Assert.That(catalogs.Components.TryLoadToken(MortimerComponentContent.ZombieReference, out var zombie),
                Is.True);
            Assert.That(zombie.SupplyCount, Is.EqualTo(4), "MO-Q1");
            foreach (var handler in MortimerComponentContent.Handlers())
            {
                Assert.That(catalogs.Capabilities.Registered, Has.Member(handler.Reference),
                    handler.Reference.ToString());
            }

            Assert.That(MortimerCardContent.NotYetComposed, Is.Empty);
            Assert.That(MortimerCardContent.AllDefinitions.Count, Is.EqualTo(19), "all nineteen fronts");
        }

        [Test]
        public void TheTierThreeStaging_UnlocksMasterOfPuppets()
        {
            var staged = MatchConfiguration.CreateStagedMortimerTierThreeForInternalTest(TeamSide.Red);
            var ultimate = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(MortimerCardContent.MasterOfPuppets));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(staged.CardInstances.Count(card =>
                card.OwnerId.Equals(ultimate.OwnerId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(5));
        }

        #if UNITY_EDITOR
        [Test]
        public void PreparedPrivateArt_LoadsForTheZombieTheStandeesAndTheSelectionFront()
        {
            Assert.That(PlacedTokenArtwork.TryLoadTexture(MortimerComponentContent.ZombieId, out _), Is.True,
                "run Tools/Prepare-TokenArtwork.ps1 -Hero mortimer in this checkout");
            Assert.That(PlacedTokenArtwork.Caption(MortimerComponentContent.Zombie, true), Is.Empty);
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/mortimer-red"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/mortimer-blue"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/HeroSelection/hero-mortimer"), Is.Not.Null);
            foreach (var definition in MortimerCardContent.DealtDefinitions)
            {
                Assert.That(ConfiguredCardArtworkCatalog.Catalog.TryGet(definition.Reference, out _),
                    Is.True, definition.DisplayName);
            }
        }
        #endif
    }
}
