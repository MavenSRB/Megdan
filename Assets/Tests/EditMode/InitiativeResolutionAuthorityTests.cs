using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class InitiativeResolutionAuthorityTests
    {
        [Test]
        public void PrintedInitiativeQuery_ReturnsCardWideTypedContribution()
        {
            var definition = CreateDefinition("query", 8);
            var card = CreateRevealedCard("query-card", definition, "red-hero-1");

            var result = PrintedValueQuery.CalculatePrintedInitiative(card, new[] { definition });

            Assert.That(result.Channel, Is.EqualTo(PrintedValueChannel.Initiative));
            Assert.That(result.Total, Is.EqualTo(8));
            Assert.That(result.Contributions, Has.Count.EqualTo(1));
            Assert.That(result.Contributions[0].Kind, Is.EqualTo(ValueContributionKind.PrintedIcon));
            Assert.That(result.Contributions[0].SourceCardInstanceId, Is.EqualTo(card.Id));
            Assert.That(result.Contributions[0].HasSourceActionDefinitionId, Is.False);
            Assert.That(result.Contributions[0].Channel, Is.EqualTo(PrintedValueChannel.Initiative));
        }

        [Test]
        public void HighestCurrentInitiative_IsRequeriedAfterEveryResolution()
        {
            var participants = CreateParticipants(2, 1);
            var firstDefinition = CreateDefinition("first", 10);
            var changingDefinition = CreateDefinition("changing", 5);
            var thirdDefinition = CreateDefinition("third", 8);
            var first = CreateRevealedCard("first-card", firstDefinition, "red-hero-1");
            var changing = CreateRevealedCard("changing-card", changingDefinition, "red-hero-2");
            var third = CreateRevealedCard("third-card", thirdDefinition, "blue-hero-1");
            var cards = new[] { third, changing, first };
            var authority = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Red));

            Assert.That(authority.TrySelectNextActor(
                new[] { firstDefinition, changingDefinition, thirdDefinition },
                cards,
                participants, null, RuleModificationSet.Empty).Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(first.Id));
            ResolveAndAcknowledge(authority, first, cards);

            var changedCurrentDefinition = CreateDefinition("changing", 9);
            Assert.That(authority.TrySelectNextActor(
                new[] { firstDefinition, changedCurrentDefinition, thirdDefinition },
                cards,
                participants, null, RuleModificationSet.Empty).Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(changing.Id));
            Assert.That(authority.SelectedActor.InitiativeTotal, Is.EqualTo(9));
        }

        [TestCase(TeamSide.Red)]
        [TestCase(TeamSide.Blue)]
        public void CrossTeamTie_SelectsTieBreakerSideAndFlipsOnlyAfterSelectedCardResolves(
            TeamSide initialSide)
        {
            var participants = CreateParticipants(1, 1);
            var redDefinition = CreateDefinition("red", 7);
            var blueDefinition = CreateDefinition("blue", 7);
            var red = CreateRevealedCard("red-card", redDefinition, "red-hero-1");
            var blue = CreateRevealedCard("blue-card", blueDefinition, "blue-hero-1");
            var cards = new[] { blue, red };
            var authority = new InitiativeResolutionAuthority(new TieBreakerState(initialSide));
            var selectedCard = initialSide == TeamSide.Red ? red : blue;
            var flippedSide = initialSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;

            var selection = authority.TrySelectNextActor(
                new[] { blueDefinition, redDefinition },
                cards,
                participants, null, RuleModificationSet.Empty);

            Assert.That(selection.Accepted, Is.True);
            Assert.That(selection.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                InitiativeResolutionEventKind.NextActorSelected,
                InitiativeResolutionEventKind.TieBreakerFlipDeferred
            }));
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(selectedCard.Id));
            Assert.That(authority.SelectedActor.FlipTieBreakerAfterResolution, Is.True);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(initialSide));

            AssertRejected(
                authority.TryAcknowledgeSelectedCardResolved(selectedCard.Id, cards),
                InitiativeResolutionRejectionReason.SelectedCardNotResolved);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(initialSide));

            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(selectedCard), Is.True);
            var completion = authority.TryAcknowledgeSelectedCardResolved(selectedCard.Id, cards);

            Assert.That(completion.Accepted, Is.True);
            Assert.That(completion.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                InitiativeResolutionEventKind.SelectedActorFinished,
                InitiativeResolutionEventKind.TieBreakerFlipped
            }));
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(flippedSide));
            Assert.That(authority.SelectedActor.IsDefined, Is.False);
        }

        [Test]
        public void SameTeamTie_RequestsAuthoritativeChoiceAndNeverFlipsTieBreaker()
        {
            var participants = CreateParticipants(3, 0);
            var definitions = Enumerable.Range(1, 3)
                .Select(index => CreateDefinition($"red-{index}", 6))
                .ToArray();
            var cards = definitions.Select((definition, index) =>
                    CreateRevealedCard($"red-card-{index + 1}", definition, $"red-hero-{index + 1}"))
                .Reverse()
                .ToArray();
            var authority = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Blue));

            var selection = authority.TrySelectNextActor(definitions, cards, participants, null, RuleModificationSet.Empty);

            Assert.That(selection.Accepted, Is.True);
            Assert.That(selection.Events.Single().Kind,
                Is.EqualTo(InitiativeResolutionEventKind.SameTeamChoiceRequested));
            Assert.That(authority.PendingChoice.OwningSide, Is.EqualTo(TeamSide.Red));
            var candidateIds = authority.PendingChoice.CandidateCardInstanceIds
                .Select(id => id.Value)
                .ToArray();
            Assert.That(candidateIds,
                Is.EqualTo(candidateIds.OrderBy(id => id, System.StringComparer.Ordinal).ToArray()));
            Assert.That(authority.PendingChoice.FlipTieBreakerAfterResolution, Is.False);
            AssertRejected(
                authority.TryChooseTiedActor(
                    TeamSide.Blue,
                    cards[0].Id,
                    definitions,
                    cards,
                    participants, null, RuleModificationSet.Empty),
                InitiativeResolutionRejectionReason.ChoiceOwnedByOtherTeam);

            var chosenCard = cards[1];
            var choice = authority.TryChooseTiedActor(
                TeamSide.Red,
                chosenCard.Id,
                definitions,
                cards,
                participants, null, RuleModificationSet.Empty);

            Assert.That(choice.Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(chosenCard.Id));
            Assert.That(authority.SelectedActor.FlipTieBreakerAfterResolution, Is.False);
            ResolveAndAcknowledge(authority, chosenCard, cards);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void MoreThanTwoCrossTeamTiedCards_ChoosesWithinTieBreakerTeamThenDefersFlip()
        {
            var participants = CreateParticipants(2, 1);
            var redOneDefinition = CreateDefinition("red-one", 9);
            var redTwoDefinition = CreateDefinition("red-two", 9);
            var blueDefinition = CreateDefinition("blue", 9);
            var redOne = CreateRevealedCard("red-one-card", redOneDefinition, "red-hero-1");
            var redTwo = CreateRevealedCard("red-two-card", redTwoDefinition, "red-hero-2");
            var blue = CreateRevealedCard("blue-card", blueDefinition, "blue-hero-1");
            var definitions = new[] { redOneDefinition, redTwoDefinition, blueDefinition };
            var cards = new[] { blue, redTwo, redOne };
            var authority = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Red));

            var selection = authority.TrySelectNextActor(definitions, cards, participants, null, RuleModificationSet.Empty);

            Assert.That(selection.Accepted, Is.True);
            Assert.That(authority.PendingChoice.OwningSide, Is.EqualTo(TeamSide.Red));
            Assert.That(authority.PendingChoice.CandidateCardInstanceIds,
                Is.EquivalentTo(new[] { redOne.Id, redTwo.Id }));
            Assert.That(authority.PendingChoice.FlipTieBreakerAfterResolution, Is.True);

            Assert.That(authority.TryChooseTiedActor(
                TeamSide.Red,
                redTwo.Id,
                definitions,
                cards,
                participants, null, RuleModificationSet.Empty).Accepted, Is.True);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));
            ResolveAndAcknowledge(authority, redTwo, cards);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void PendingChoice_RevalidatesCurrentInitiativeBeforeAcceptingAnswer()
        {
            var participants = CreateParticipants(2, 0);
            var firstDefinition = CreateDefinition("first", 5);
            var secondDefinition = CreateDefinition("second", 5);
            var first = CreateRevealedCard("first-card", firstDefinition, "red-hero-1");
            var second = CreateRevealedCard("second-card", secondDefinition, "red-hero-2");
            var cards = new[] { first, second };
            var authority = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Red));
            Assert.That(authority.TrySelectNextActor(
                new[] { firstDefinition, secondDefinition },
                cards,
                participants, null, RuleModificationSet.Empty).Accepted, Is.True);

            var changedSecondDefinition = CreateDefinition("second", 6);
            AssertRejected(
                authority.TryChooseTiedActor(
                    TeamSide.Red,
                    first.Id,
                    new[] { firstDefinition, changedSecondDefinition },
                    cards,
                    participants, null, RuleModificationSet.Empty),
                InitiativeResolutionRejectionReason.InitiativeStateChanged);

            Assert.That(authority.PendingChoice, Is.Not.Null);
            Assert.That(authority.SelectedActor.IsDefined, Is.False);
            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));
        }

        [Test]
        public void NoUnresolvedCards_ReportsCompletionWithoutSelectingActor()
        {
            var participants = CreateParticipants(1, 1);
            var definition = CreateDefinition("unused", 4);
            var handCard = CardInstance.CreateInHand(
                new CardInstanceId("unused-card"),
                definition.Reference,
                new HeroId("red-hero-1"));
            var authority = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Blue));

            var result = authority.TrySelectNextActor(
                new[] { definition },
                new[] { handCard },
                participants, null, RuleModificationSet.Empty);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events.Single().Kind,
                Is.EqualTo(InitiativeResolutionEventKind.NoUnresolvedCardsRemaining));
            Assert.That(authority.SelectedActor.IsDefined, Is.False);
        }

        private static CardDefinition CreateDefinition(string id, int initiative)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "initiative-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated);
        }

        private static CardInstance CreateRevealedCard(
            string cardId,
            CardDefinition definition,
            string ownerId)
        {
            var card = CardInstance.CreateInHand(
                new CardInstanceId(cardId),
                definition.Reference,
                new HeroId(ownerId));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            return card;
        }

        private static MatchParticipants CreateParticipants(int redCount, int blueCount)
        {
            var redTeamId = new TeamId("red-team");
            var blueTeamId = new TeamId("blue-team");
            var teams = new List<Team>();
            var players = new List<Player>();
            var heroes = new List<Hero>();
            if (redCount > 0)
            {
                teams.Add(new Team(redTeamId, TeamSide.Red));
            }

            if (blueCount > 0)
            {
                teams.Add(new Team(blueTeamId, TeamSide.Blue));
            }

            AddSide(redCount, "red", redTeamId, players, heroes);
            AddSide(blueCount, "blue", blueTeamId, players, heroes);
            return new MatchParticipants(teams, players, heroes);
        }

        private static void AddSide(
            int count,
            string prefix,
            TeamId teamId,
            ICollection<Player> players,
            ICollection<Hero> heroes)
        {
            for (var index = 1; index <= count; index++)
            {
                var playerId = new PlayerId($"{prefix}-player-{index}");
                players.Add(new Player(playerId, teamId));
                heroes.Add(new Hero(new HeroId($"{prefix}-hero-{index}"), playerId, HeroDashboard.RulebookLayout));
            }
        }

        private static void ResolveAndAcknowledge(
            InitiativeResolutionAuthority authority,
            CardInstance card,
            IEnumerable<CardInstance> cards)
        {
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
            Assert.That(authority.TryAcknowledgeSelectedCardResolved(card.Id, cards).Accepted, Is.True);
        }

        private static void AssertRejected(
            InitiativeResolutionResult result,
            InitiativeResolutionRejectionReason reason)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(reason));
            Assert.That(result.Events, Is.Empty);
        }
    }
}
