using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.49. Rulebook p.13: "Defeated hero's team spends the number of Life counters, equal
    /// to that hero's lowest card Tier", and "your friendly heroes collect assist coins based on the
    /// defeated hero's lowest card Tier" - the table beneath them mapping Level 1-3 to Tier I, 4-6 to
    /// Tier II and 7-8 to Tier III.
    ///
    /// <para><b>Found at the table, 2026-09-18:</b> "Lives are not being counted correctly. It just
    /// takes -1 from pool." p.15's Upgrade a Card returns the upgraded card to the DECK, face up, and
    /// it stays there for the rest of the game - so a hero that has levelled to 4 and holds three
    /// Tier II cards still owns three Tier I cards in its deck. The whole-pool reading milestone 7.4
    /// shipped found them, answered Tier I for every hero at every Level, and priced every Defeat at
    /// one counter and one assist coin. The reading p.13's table reproduces is over the cards the hero
    /// is playing with: hand, turn area and discard pile.</para>
    /// </summary>
    [TestFixture]
    public sealed class HeroDefeatCostQueryTests
    {
        private static readonly HeroId Hero = new HeroId("swift-owner");

        [Test]
        public void ATierOneCardReturnedToTheDeckByAnUpgrade_DoesNotLowerTheHerosTier()
        {
            var tierTwo = NonBasic(CardTier.II);
            var returned = NonBasic(CardTier.I);
            var instances = new[]
            {
                CardInstance.CreateInHand(new CardInstanceId("held"), tierTwo.Reference, Hero),
                // p.15: "Return this card to your deck" - CardLifecycleAuthority.TryReturnToDeck
                // leaves it face up, exactly as the setup's own deck cards are.
                CardInstance.CreateInDeck(new CardInstanceId("returned"), returned.Reference, Hero)
            };

            Assert.That(HeroDefeatCostQuery.TryGetLowestCardTier(
                    Hero, SwiftCardContent.DealtDefinitions, instances, out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(CardTier.II),
                "The card the hero gave back to the deck is not a card the hero is playing with.");
            Assert.That(HeroDefeatCostQuery.Worth(Hero, SwiftCardContent.DealtDefinitions, instances),
                Is.EqualTo(2), "p.13's table: Tier II is two assist coins and two Life counters.");

            // The reading this milestone replaced, kept here as the measurement of the defect: over
            // every location the returned Tier I card wins, and the hero reads as Level 1 forever.
            Assert.That(CardCharacteristicQuery.TryGetLowestNonBasicTier(
                    new CardCharacteristicCriteria(ownerId: Hero),
                    SwiftCardContent.DealtDefinitions, instances, out var wholePool), Is.True);
            Assert.That(wholePool, Is.EqualTo(CardTier.I));
        }

        [Test]
        public void ThePlayedAndDiscardedCardsStillCount_SoADefeatMidRoundReadsTheWholeHand()
        {
            var tierTwo = NonBasic(CardTier.II);
            var tierThree = NonBasic(CardTier.III);
            var played = CardInstance.CreateInHand(
                new CardInstanceId("played"), tierTwo.Reference, Hero);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(played), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(played), Is.True);
            var instances = new[]
            {
                played,
                CardInstance.CreateInHand(new CardInstanceId("held"), tierThree.Reference, Hero)
            };

            Assert.That(HeroDefeatCostQuery.TryGetLowestCardTier(
                    Hero, SwiftCardContent.DealtDefinitions, instances, out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(CardTier.II),
                "A card in the turn area is one the hero is playing with this round.");
        }

        [Test]
        public void AHeroPlayingWithBasicsAlone_HasNoTierToRead()
        {
            var basic = SwiftCardContent.Definition(SwiftCardContent.Bounce);
            var returned = NonBasic(CardTier.I);
            var instances = new[]
            {
                CardInstance.CreateInHand(new CardInstanceId("basic"), basic.Reference, Hero),
                CardInstance.CreateInDeck(new CardInstanceId("in-deck"), returned.Reference, Hero)
            };

            Assert.That(HeroDefeatCostQuery.TryGetLowestCardTier(
                    Hero, SwiftCardContent.DealtDefinitions, instances, out _), Is.False,
                "p.15: the Basic pair is \"not considered when checking your lowest card Tier\", and a " +
                "deck card is not in play.");
            Assert.That(HeroDefeatCostQuery.Worth(Hero, SwiftCardContent.DealtDefinitions, instances),
                Is.Zero);
        }

        private static CardDefinition NonBasic(CardTier tier) =>
            SwiftCardContent.DealtDefinitions.First(definition =>
                definition.Progression.IsNonBasic && definition.Progression.Tier == tier);
    }
}
