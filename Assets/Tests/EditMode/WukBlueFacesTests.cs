using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WukRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114c. Wuk's Blue retrieval faces through a configured round: the mandatory Tree
    /// removal in the printed Radius, Wuk's optional retrieval, a friendly hero's retrieval on their
    /// own choice, Tree of Plenty's one bullet and Abundance's one or both.
    /// </summary>
    public sealed class WukBlueFacesTests
    {
        private static CardDefinition Gifts => Face(WukBlueFaces.GiftsOfNature);
        private static CardDefinition Plenty => Face(WukBlueFaces.TreeOfPlenty);
        private static CardDefinition Abundance => Face(WukBlueFaces.Abundance);

        private static CardDefinition Face(CardDefinitionId id) =>
            WukCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        private static readonly CardInstanceId WuksDiscard = new CardInstanceId("wuk-discarded-0");
        private static readonly CardInstanceId AllysDiscard = new CardInstanceId("wuk-ally-discarded-0");

        [Test]
        public void Gifts_RemovesATreeInItsPrintedRadiusThenWukMayRetrieve()
        {
            var round = new WukRound(Gifts, treesAt: new[] { H(0, 3), H(0, 4) }, wukDiscards: 1);
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo(WukBlueFaces.TreeNode));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(0, 3) }),
                "Gifts of Nature prints Radius 3: the Tree 4 away is out of reach.");
            round.PickSpaces(H(0, 3));
            Assert.That(round.Trees(), Is.EqualTo(new[] { H(0, 4) }), "the chosen Tree is removed.");

            round.Accept();
            Assert.That(round.OfferedCards(), Is.EqualTo(new[] { WuksDiscard }));
            round.PickCard(WuksDiscard);
            Assert.That(round.LocationsOf(Wuk), Has.No.Member(CardLocation.DiscardPile),
                "the discarded card came back to his hand.");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Gifts_WukMayDeclineTheRetrieval()
        {
            var round = new WukRound(Gifts, treesAt: new[] { H(1, 0) }, wukDiscards: 1);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.Decline();

            Assert.That(round.Trees(), Is.Empty);
            Assert.That(round.LocationsOf(Wuk), Has.Member(CardLocation.DiscardPile),
                "\"You may retrieve\": declined, the card stays discarded.");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Gifts_WithNoTreeInRadius_IsNotOffered()
        {
            var round = new WukRound(Gifts, treesAt: new[] { H(0, 4) }, wukDiscards: 1);

            Assert.That(round.IsOffered(WukCardContent.PrimarySkill), Is.False,
                "p.10: \"Remove a Tree token\" is mandatory; with none in reach the face stops before " +
                "the retrieval, so it can do nothing and is not offered.");
        }

        [Test]
        public void Plenty_AFriendlyHeroInRadiusRetrievesOnTheirOwnChoice()
        {
            var round = new WukRound(Plenty, allyAt: H(0, 4), treesAt: new[] { H(1, 0) },
                allyDiscards: 1);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));

            Assert.That(round.PendingNode, Is.EqualTo(WukBlueFaces.ModalNode));
            round.TakeBullets(WukBlueFaces.FriendBulletNode);
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { Ally }),
                "a friendly hero in Tree of Plenty's Radius 4 - never Wuk himself (p.17).");
            round.PickHero(Ally);

            if (round.Chooser.Equals(Wuk))
            {
                // The enclosing `may` opens its confirm to the card's controller - the bounded
                // fidelity note Silverarrow's Blessing records.
                round.Accept();
            }

            Assert.That(round.Chooser, Is.EqualTo(Ally), "the retrieving hero chooses their own card.");
            Assert.That(round.OfferedCards(), Is.EqualTo(new[] { AllysDiscard }));
            round.AssertRestoreRepliesWithCard(AllysDiscard);
            Assert.That(round.LocationsOf(Ally), Has.No.Member(CardLocation.DiscardPile));
        }

        [Test]
        public void Plenty_WithNoFriendlyHeroInRadius_TheFriendBulletIsGreyed()
        {
            var round = new WukRound(Plenty, allyAt: H(0, 5), treesAt: new[] { H(1, 0) }, wukDiscards: 1);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));

            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { WukBlueFaces.FriendBulletNode }),
                "the ally stands 5 away, outside Radius 4.");
            Assert.That(round.AvailableBullets(), Is.EqualTo(new[] { WukBlueFaces.SelfBulletNode }));
        }

        [Test]
        public void Abundance_TakesBothRetrievals()
        {
            var round = new WukRound(Abundance, allyAt: H(-2, 0), treesAt: new[] { H(1, 0) },
                wukDiscards: 1, allyDiscards: 1);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.TakeBullets(WukBlueFaces.SelfBulletNode, WukBlueFaces.FriendBulletNode);

            for (var step = 0; step < 8 && !round.ActionEnded; step++)
            {
                var cards = round.OfferedCards();
                var heroes = round.OfferedHeroes();
                if (cards.Length == 1) { round.PickCard(cards[0]); continue; }
                if (heroes.Length == 1) { round.PickHero(heroes[0]); continue; }
                round.Accept();
            }

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.LocationsOf(Wuk), Has.No.Member(CardLocation.DiscardPile));
            Assert.That(round.LocationsOf(Ally), Has.No.Member(CardLocation.DiscardPile),
                "\"Choose one or both\": Wuk and his ally each retrieved a card.");
        }
    }
}
