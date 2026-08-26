using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TurnCardCommitmentAuthorityTests
    {
        [Test]
        public void EveryFiveCardHandSlot_CanCommitWhenEligibleAndCannotCommitAfterDefenseDiscard()
        {
            for (var index = 0; index < 5; index++)
            {
                var eligible = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                var selected = eligible.RedHand.Instances[index];
                var authority = CreateAuthority(eligible);
                Assert.That(authority.TryCommitCard(selected.OwnerId, selected.Id).Accepted, Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(selected), Is.True);
                Assert.That(ActionOfferQuery.GetAvailableTurnActionOffers(
                        eligible.CardDefinitions,
                        eligible.CardInstances,
                        selected.OwnerId,
                        selected.Id),
                    Has.Count.EqualTo(2));

                var discarded = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                var discardedCard = discarded.BlueHand.Instances[index];
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(discardedCard), Is.True);
                var rejected = CreateAuthority(discarded)
                    .TryCommitCard(discardedCard.OwnerId, discardedCard.Id);
                AssertRejected(rejected, TurnCardCommitmentRejectionReason.CardNotEligible);
            }
        }

        [Test]
        public void EveryConfiguredHero_MustRespondBeforeDeterministicallyOrderedJointReveal()
        {
            var fixture = CreateThreeHeroFixture();
            var authority = new TurnCardCommitmentAuthority(fixture.Cards, fixture.Participants);

            var zed = authority.TryCommitCard(fixture.ZedHeroId, fixture.ZedCard.Id);
            var alpha = authority.TryCommitCard(fixture.AlphaHeroId, fixture.AlphaCard.Id);
            Assert.That(zed.Accepted && alpha.Accepted, Is.True);
            Assert.That(authority.IsRevealed, Is.False);
            Assert.That(fixture.ZedCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
            Assert.That(fixture.AlphaCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));

            var middle = authority.TryCommitCard(fixture.MiddleHeroId, fixture.MiddleCard.Id);

            Assert.That(middle.Accepted, Is.True);
            Assert.That(middle.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                TurnCardCommitmentEventKind.CardCommittedFacedown,
                TurnCardCommitmentEventKind.CardRevealed,
                TurnCardCommitmentEventKind.CardRevealed,
                TurnCardCommitmentEventKind.CardRevealed,
                TurnCardCommitmentEventKind.RevealCompleted
            }));
            Assert.That(middle.Events.Where(ruleEvent =>
                    ruleEvent.Kind == TurnCardCommitmentEventKind.CardRevealed)
                .Select(ruleEvent => ruleEvent.HeroId.Value),
                Is.EqualTo(new[] { "hero-alpha", "hero-middle", "hero-zed" }));
            Assert.That(authority.ExpectedHeroIds.Select(heroId => heroId.Value),
                Is.EqualTo(new[] { "hero-alpha", "hero-middle", "hero-zed" }));
            Assert.That(authority.IsRevealed, Is.True);
            Assert.That(fixture.Cards, Has.All.Matches<CardInstance>(card =>
                card.State.FaceOrientation == CardFaceOrientation.FaceUp));
        }

        [Test]
        public void Commit_IsHeroOwnedAndRejectsUnknownCrossOwnerMissingRepeatedAndStaleState()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = CreateAuthority(configuration);
            var red = configuration.RedHand.Instances[0];
            var blue = configuration.BlueHand.Instances[0];
            var statesBefore = configuration.CardInstances.ToDictionary(card => card.Id, card => card.State);

            AssertRejected(
                authority.TryCommitCard(new HeroId("unknown-hero"), red.Id),
                TurnCardCommitmentRejectionReason.HeroNotFound);
            AssertRejected(
                authority.TryCommitCard(red.OwnerId, blue.Id),
                TurnCardCommitmentRejectionReason.CardNotOwnedByHero);
            AssertRejected(
                authority.TryCommitCard(red.OwnerId, new CardInstanceId("missing-card")),
                TurnCardCommitmentRejectionReason.CardNotFound);
            Assert.That(configuration.CardInstances, Has.All.Matches<CardInstance>(card =>
                card.State.Equals(statesBefore[card.Id])));

            Assert.That(authority.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
            AssertRejected(
                authority.TryCommitCard(red.OwnerId, configuration.RedHand.Instances[1].Id),
                TurnCardCommitmentRejectionReason.HeroAlreadyResponded);
            Assert.That(CardLifecycleAuthority.TryReveal(red), Is.True);
            AssertRejected(
                authority.TryCommitCard(blue.OwnerId, blue.Id),
                TurnCardCommitmentRejectionReason.CommittedCardStateChanged);
        }

        [Test]
        public void Pass_RequiresAnActuallyEmptyOwnedHandEvenWhenHeroHasNoBoardPresence()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = CreateAuthority(configuration);
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;

            Assert.That(authority.CanPass(redHeroId), Is.False,
                "Board presence is not an input and cannot relax the mandatory card response.");
            AssertRejected(
                authority.TryPass(redHeroId),
                TurnCardCommitmentRejectionReason.PassNotAllowed);

            foreach (var card in configuration.RedHand.Instances)
            {
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
            }

            Assert.That(authority.CanPass(redHeroId), Is.True);
            var pass = authority.TryPass(redHeroId);
            Assert.That(pass.Accepted, Is.True);
            Assert.That(pass.Events.Single().Kind, Is.EqualTo(TurnCardCommitmentEventKind.HeroPassed));
            Assert.That(pass.Events.Single().HeroId, Is.EqualTo(redHeroId));
            Assert.That(authority.GetStatus(redHeroId), Is.EqualTo(TurnCardCommitmentStatus.Passed));
        }

        [Test]
        public void InvalidInHandLifecycleState_IsAuthorityErrorRatherThanEmptyHandPass()
        {
            var participants = StagedBoardParticipants.Create();
            var redHeroId = GetHeroId(participants, TeamSide.Red);
            var invalid = new CardInstance(
                new CardInstanceId("invalid-red-hand-card"),
                new CardDefinitionReference(new CardDefinitionId("invalid-definition"), "rev-1"),
                redHeroId,
                new CardStateFacts(
                    CardLocation.Hand,
                    true,
                    CardResolutionStatus.Resolved,
                    CardFaceOrientation.FaceUp));
            var authority = new TurnCardCommitmentAuthority(new[] { invalid }, participants);

            Assert.That(authority.CanPass(redHeroId), Is.False);
            AssertRejected(
                authority.TryPass(redHeroId),
                TurnCardCommitmentRejectionReason.InvalidInHandCardState);
            AssertRejected(
                authority.TryCommitCard(redHeroId, invalid.Id),
                TurnCardCommitmentRejectionReason.InvalidInHandCardState);
            Assert.That(invalid.State.Location, Is.EqualTo(CardLocation.Hand));
        }

        [Test]
        public void Constructor_RejectsDuplicateCardIdentityAndBrokenOwnerReference()
        {
            var participants = StagedBoardParticipants.Create();
            var definition = new CardDefinitionReference(
                new CardDefinitionId("test-definition"),
                "rev-1");
            var duplicateId = new CardInstanceId("duplicate-card");
            var redHeroId = GetHeroId(participants, TeamSide.Red);

            Assert.Throws<ArgumentException>(() => new TurnCardCommitmentAuthority(
                new[]
                {
                    CardInstance.CreateInHand(duplicateId, definition, redHeroId),
                    CardInstance.CreateInHand(duplicateId, definition, redHeroId)
                },
                participants));
            Assert.Throws<KeyNotFoundException>(() => new TurnCardCommitmentAuthority(
                new[]
                {
                    CardInstance.CreateInHand(
                        new CardInstanceId("unknown-owner-card"),
                        definition,
                        new HeroId("unknown-owner"))
                },
                participants));
        }

        [Test]
        public void SameHeroCommitSequence_ReplaysWithIdenticalStatesAndOrderedEvents()
        {
            var first = RunCommitSequence();
            var second = RunCommitSequence();

            Assert.That(second.States, Is.EqualTo(first.States));
            Assert.That(second.EventKinds, Is.EqualTo(first.EventKinds));
            Assert.That(second.EventHeroIds, Is.EqualTo(first.EventHeroIds));
            Assert.That(second.EventCardIds, Is.EqualTo(first.EventCardIds));
        }

        private static TurnCardCommitmentAuthority CreateAuthority(
            MatchConfiguration configuration)
        {
            return new TurnCardCommitmentAuthority(
                configuration.CardInstances,
                configuration.Participants);
        }

        private static void AssertRejected(
            TurnCardCommitmentResult result,
            TurnCardCommitmentRejectionReason reason)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(reason));
            Assert.That(result.Events, Is.Empty);
        }

        private static HeroId GetHeroId(MatchParticipants participants, TeamSide side)
        {
            var team = participants.Teams.Single(candidate => candidate.Side == side);
            return participants.GetHeroesForTeam(team.Id).Single().Id;
        }

        private static ReplayFacts RunCommitSequence()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var authority = CreateAuthority(configuration);
            var events = authority.TryCommitCard(
                    configuration.BlueHand.Instances[0].OwnerId,
                    configuration.BlueHand.Instances[1].Id)
                .Events
                .Concat(authority.TryCommitCard(
                    configuration.RedHand.Instances[0].OwnerId,
                    configuration.RedHand.Instances[0].Id).Events)
                .ToArray();
            return new ReplayFacts(
                configuration.CardInstances
                    .OrderBy(card => card.Id.Value)
                    .Select(card => card.State)
                    .ToArray(),
                events.Select(ruleEvent => ruleEvent.Kind).ToArray(),
                events.Select(ruleEvent => ruleEvent.HeroId).ToArray(),
                events.Select(ruleEvent => ruleEvent.CardInstanceId).ToArray());
        }

        private static ThreeHeroFixture CreateThreeHeroFixture()
        {
            var redTeamId = new TeamId("team-red");
            var blueTeamId = new TeamId("team-blue");
            var alphaPlayerId = new PlayerId("player-alpha");
            var middlePlayerId = new PlayerId("player-middle");
            var zedPlayerId = new PlayerId("player-zed");
            var alphaHeroId = new HeroId("hero-alpha");
            var middleHeroId = new HeroId("hero-middle");
            var zedHeroId = new HeroId("hero-zed");
            var participants = new MatchParticipants(
                new[]
                {
                    new Team(redTeamId, TeamSide.Red),
                    new Team(blueTeamId, TeamSide.Blue)
                },
                new[]
                {
                    new Player(zedPlayerId, redTeamId),
                    new Player(alphaPlayerId, blueTeamId),
                    new Player(middlePlayerId, redTeamId)
                },
                new[]
                {
                    new Hero(zedHeroId, zedPlayerId, HeroDashboard.RulebookLayout),
                    new Hero(alphaHeroId, alphaPlayerId, HeroDashboard.RulebookLayout),
                    new Hero(middleHeroId, middlePlayerId, HeroDashboard.RulebookLayout)
                });
            var definition = new CardDefinitionReference(
                new CardDefinitionId("three-hero-definition"),
                "rev-1");
            var alphaCard = CardInstance.CreateInHand(
                new CardInstanceId("card-alpha"), definition, alphaHeroId);
            var middleCard = CardInstance.CreateInHand(
                new CardInstanceId("card-middle"), definition, middleHeroId);
            var zedCard = CardInstance.CreateInHand(
                new CardInstanceId("card-zed"), definition, zedHeroId);
            return new ThreeHeroFixture(
                participants,
                new[] { zedCard, alphaCard, middleCard },
                alphaHeroId,
                middleHeroId,
                zedHeroId,
                alphaCard,
                middleCard,
                zedCard);
        }

        private sealed class ThreeHeroFixture
        {
            public ThreeHeroFixture(
                MatchParticipants participants,
                CardInstance[] cards,
                HeroId alphaHeroId,
                HeroId middleHeroId,
                HeroId zedHeroId,
                CardInstance alphaCard,
                CardInstance middleCard,
                CardInstance zedCard)
            {
                Participants = participants;
                Cards = cards;
                AlphaHeroId = alphaHeroId;
                MiddleHeroId = middleHeroId;
                ZedHeroId = zedHeroId;
                AlphaCard = alphaCard;
                MiddleCard = middleCard;
                ZedCard = zedCard;
            }

            public MatchParticipants Participants { get; }
            public CardInstance[] Cards { get; }
            public HeroId AlphaHeroId { get; }
            public HeroId MiddleHeroId { get; }
            public HeroId ZedHeroId { get; }
            public CardInstance AlphaCard { get; }
            public CardInstance MiddleCard { get; }
            public CardInstance ZedCard { get; }
        }

        private sealed class ReplayFacts
        {
            public ReplayFacts(
                CardStateFacts[] states,
                TurnCardCommitmentEventKind[] eventKinds,
                HeroId[] eventHeroIds,
                CardInstanceId[] eventCardIds)
            {
                States = states;
                EventKinds = eventKinds;
                EventHeroIds = eventHeroIds;
                EventCardIds = eventCardIds;
            }

            public CardStateFacts[] States { get; }
            public TurnCardCommitmentEventKind[] EventKinds { get; }
            public HeroId[] EventHeroIds { get; }
            public CardInstanceId[] EventCardIds { get; }
        }
    }
}
