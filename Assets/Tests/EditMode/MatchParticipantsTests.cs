using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MatchParticipantsTests
    {
        [Test]
        public void PlaceholderConfiguration_CreatesOneRedAndOneBlueHeroWithStableRelationships()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var participants = configuration.Participants;

            Assert.That(participants.Teams, Has.Count.EqualTo(2));
            Assert.That(participants.Players, Has.Count.EqualTo(2));
            Assert.That(participants.Heroes, Has.Count.EqualTo(2));

            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            Assert.That(redHeroId.Value, Is.EqualTo("primitive-hero"));
            Assert.That(participants.GetTeamForHero(redHeroId).Side, Is.EqualTo(TeamSide.Red));
            Assert.That(participants.GetTeamForHero(blueHeroId).Side, Is.EqualTo(TeamSide.Blue));
            Assert.That(participants.GetPlayerForHero(redHeroId).Id.Value, Is.EqualTo(PlaceholderOneVsOneParticipants.RedPlayerIdValue));
            Assert.That(participants.GetPlayerForHero(blueHeroId).Id.Value, Is.EqualTo(PlaceholderOneVsOneParticipants.BluePlayerIdValue));
        }

        [Test]
        public void PlaceholderConfiguration_GivesEachHeroFiveDistinctOwnedPlayableCards()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

            Assert.That(configuration.RedHand.DealtDefinitions, Has.Count.EqualTo(5));
            Assert.That(configuration.RedHand.Instances, Has.Count.EqualTo(5));
            Assert.That(configuration.RedHand.Instances.All(card => card.OwnerId.Equals(redHeroId)), Is.True);
            Assert.That(configuration.BlueHand.DealtDefinitions, Has.Count.EqualTo(5));
            Assert.That(configuration.BlueHand.Instances, Has.Count.EqualTo(5));
            Assert.That(configuration.BlueHand.Instances.All(card => card.OwnerId.Equals(blueHeroId)), Is.True);

            // Milestone 7.1. Ten cards are dealt and thirty-four definitions exist, because each of the
            // six non-Basic Tier I cards supplies the four same-colour cards p.15 has it upgrade into -
            // two Tier II and the two Tier III both of those lead to. None of the twenty-four is dealt:
            // p.15 finds them in the player's own deck, and milestone 7.5 is the first that moves one.
            Assert.That(configuration.RedHand.Definitions, Has.Count.EqualTo(17));
            Assert.That(configuration.BlueHand.Definitions, Has.Count.EqualTo(17));
            Assert.That(configuration.CardDefinitions, Has.Count.EqualTo(34));
            Assert.That(configuration.CardDefinitions.Select(card => card.Id).Distinct().Count(), Is.EqualTo(34));
            Assert.That(configuration.CardInstances.Select(card => card.Id).Distinct().Count(), Is.EqualTo(10));

            foreach (var card in configuration.CardInstances)
            {
                Assert.That(configuration.Participants.GetHero(card.OwnerId).Id, Is.EqualTo(card.OwnerId));
                Assert.That(configuration.CardDefinitions.Any(definition => definition.Reference.Equals(card.DefinitionReference)), Is.True);
            }
        }

        [Test]
        public void OriginalPhaseOneCardOwners_StillResolveToTheStableRedHero()
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var phaseOneHand = PlaceholderMovementHand.Create(redHeroId);

            foreach (var card in phaseOneHand.Instances)
            {
                Assert.That(participants.GetHero(card.OwnerId).Id, Is.EqualTo(redHeroId));
                Assert.That(participants.GetTeamForHero(card.OwnerId).Side, Is.EqualTo(TeamSide.Red));
            }
        }

        [Test]
        public void TeamQueries_ReturnCollectionsWithMoreThanOneHeroPerTeam()
        {
            var redTeamId = new TeamId("red-team");
            var blueTeamId = new TeamId("blue-team");
            var redOne = new PlayerId("red-one");
            var redTwo = new PlayerId("red-two");
            var blueOne = new PlayerId("blue-one");
            var blueTwo = new PlayerId("blue-two");
            var participants = new MatchParticipants(
                new[]
                {
                    new Team(redTeamId, TeamSide.Red),
                    new Team(blueTeamId, TeamSide.Blue)
                },
                new[]
                {
                    new Player(redOne, redTeamId),
                    new Player(redTwo, redTeamId),
                    new Player(blueOne, blueTeamId),
                    new Player(blueTwo, blueTeamId)
                },
                new[]
                {
                    new Hero(new HeroId("red-hero-one"), redOne),
                    new Hero(new HeroId("red-hero-two"), redTwo),
                    new Hero(new HeroId("blue-hero-one"), blueOne),
                    new Hero(new HeroId("blue-hero-two"), blueTwo)
                });

            Assert.That(participants.GetPlayersForTeam(redTeamId), Has.Count.EqualTo(2));
            Assert.That(participants.GetHeroesForTeam(redTeamId), Has.Count.EqualTo(2));
            Assert.That(participants.GetPlayersForTeam(blueTeamId), Has.Count.EqualTo(2));
            Assert.That(participants.GetHeroesForTeam(blueTeamId), Has.Count.EqualTo(2));
        }

        [Test]
        public void StableIds_RejectBlankValues()
        {
            Assert.Throws<ArgumentException>(() => new TeamId(" "));
            Assert.Throws<ArgumentException>(() => new PlayerId(" "));
            Assert.Throws<ArgumentException>(() => new HeroId(" "));
        }

        [Test]
        public void Collections_RejectDuplicateTeamPlayerAndHeroIds()
        {
            var redTeam = new Team(new TeamId("red-team"), TeamSide.Red);
            var blueTeam = new Team(new TeamId("blue-team"), TeamSide.Blue);
            var redPlayer = new Player(new PlayerId("red-player"), redTeam.Id);
            var bluePlayer = new Player(new PlayerId("blue-player"), blueTeam.Id);
            var redHero = new Hero(new HeroId("red-hero"), redPlayer.Id);
            var blueHero = new Hero(new HeroId("blue-hero"), bluePlayer.Id);

            Assert.Throws<ArgumentException>(() => new MatchParticipants(
                new[] { redTeam, new Team(redTeam.Id, TeamSide.Blue) },
                new[] { redPlayer, bluePlayer },
                new[] { redHero, blueHero }));
            Assert.Throws<ArgumentException>(() => new MatchParticipants(
                new[] { redTeam, blueTeam },
                new[] { redPlayer, new Player(redPlayer.Id, blueTeam.Id) },
                new[] { redHero, blueHero }));
            Assert.Throws<ArgumentException>(() => new MatchParticipants(
                new[] { redTeam, blueTeam },
                new[] { redPlayer, bluePlayer },
                new[] { redHero, new Hero(redHero.Id, bluePlayer.Id) }));
        }

        [Test]
        public void Collections_RejectUnresolvedTeamAndPlayerReferences()
        {
            var redTeam = new Team(new TeamId("red-team"), TeamSide.Red);
            var redPlayer = new Player(new PlayerId("red-player"), redTeam.Id);

            Assert.Throws<ArgumentException>(() => new MatchParticipants(
                new[] { redTeam },
                new[] { new Player(redPlayer.Id, new TeamId("missing-team")) },
                new[] { new Hero(new HeroId("red-hero"), redPlayer.Id) }));
            Assert.Throws<ArgumentException>(() => new MatchParticipants(
                new[] { redTeam },
                new[] { redPlayer },
                new[] { new Hero(new HeroId("red-hero"), new PlayerId("missing-player")) }));
        }

        [Test]
        public void Collections_RequireExactlyOneHeroForEveryPlayer()
        {
            var redTeam = new Team(new TeamId("red-team"), TeamSide.Red);
            var firstPlayer = new Player(new PlayerId("first-player"), redTeam.Id);
            var secondPlayer = new Player(new PlayerId("second-player"), redTeam.Id);

            Assert.Throws<ArgumentException>(() => new MatchParticipants(
                new[] { redTeam },
                new[] { firstPlayer, secondPlayer },
                new[]
                {
                    new Hero(new HeroId("first-hero"), firstPlayer.Id),
                    new Hero(new HeroId("second-hero"), firstPlayer.Id)
                }));
        }
    }
}
