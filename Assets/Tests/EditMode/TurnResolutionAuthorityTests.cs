using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TurnResolutionAuthorityTests
    {
        [Test]
        public void TwoUnresolvedCards_PerformHoldInInitiativeOrderAndEndTurn()
        {
            var fixture = CreateFixture(7, 4, TeamSide.Red);

            var start = fixture.Authority.TryStart();

            Assert.That(start.Accepted, Is.True);
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Red.Id));

            var firstHold = fixture.Authority.TryPerformHold(fixture.Red.Id);

            Assert.That(firstHold.Accepted, Is.True);
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Blue.Id));
            Assert.That(firstHold.Events.Any(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.TurnEnded), Is.False);

            var secondHold = fixture.Authority.TryPerformHold(fixture.Blue.Id);

            Assert.That(secondHold.Accepted, Is.True);
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.Authority.SelectedActor.IsDefined, Is.False);
            Assert.That(fixture.Authority.IsEnded, Is.True);
            Assert.That(secondHold.Events.Select(ruleEvent => ruleEvent.Kind).TakeLast(2), Is.EqualTo(new[]
            {
                TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint,
                TurnResolutionEventKind.TurnEnded
            }));

            var holdEvent = firstHold.Events.Single(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.HoldActionPerformed);
            Assert.That(holdEvent.ActionDefinitionId,
                Is.EqualTo(GeneralRuleActionOfferQuery.HoldDefinition.Id));
            Assert.That(holdEvent.InvocationContext,
                Is.EqualTo(ActionInvocationContext.GeneralRuleSecondary));

            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Blue.Id),
                TurnResolutionRejectionReason.TurnAlreadyEnded);
        }

        [Test]
        public void CrossTeamTie_FlipsAfterResolvedFactAndBeforeNextActorSelection()
        {
            var fixture = CreateFixture(7, 7, TeamSide.Red);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Red.Id));
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));

            var result = fixture.Authority.TryPerformHold(fixture.Red.Id);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Blue.Id));
            var kinds = EventKinds(result).ToArray();
            Assert.That(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.TieBreakerFlipped),
                Is.GreaterThan(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.SelectedActorFinished)));
            Assert.That(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.NextActorSelected),
                Is.GreaterThan(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.TieBreakerFlipped)));
        }

        [Test]
        public void AfterHold_NextActorIsRequeriedFromCurrentUnresolvedCards()
        {
            var participants = CreateParticipants(2, 1);
            var firstDefinition = CreateDefinition("first", 10);
            var lowerDefinition = CreateDefinition("lower", 5);
            var nextDefinition = CreateDefinition("next", 8);
            var first = CreateRevealedCard("first-card", firstDefinition, "red-hero-1");
            var lower = CreateRevealedCard("lower-card", lowerDefinition, "red-hero-2");
            var next = CreateRevealedCard("next-card", nextDefinition, "blue-hero-1");
            var authority = new TurnResolutionAuthority(
                new[] { lowerDefinition, nextDefinition, firstDefinition },
                new[] { lower, next, first },
                participants,
                new TieBreakerState(TeamSide.Red));
            Assert.That(authority.TryStart().Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(first.Id));

            Assert.That(authority.TryPerformHold(first.Id).Accepted, Is.True);

            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(next.Id));
            Assert.That(authority.SelectedActor.InitiativeTotal, Is.EqualTo(8));
        }

        [Test]
        public void SameTeamTie_UsesTeamChoiceWithoutFlippingTieBreaker()
        {
            var participants = CreateParticipants(2, 0);
            var firstDefinition = CreateDefinition("first", 6);
            var secondDefinition = CreateDefinition("second", 6);
            var first = CreateRevealedCard("first-card", firstDefinition, "red-hero-1");
            var second = CreateRevealedCard("second-card", secondDefinition, "red-hero-2");
            var authority = new TurnResolutionAuthority(
                new[] { firstDefinition, secondDefinition },
                new[] { second, first },
                participants,
                new TieBreakerState(TeamSide.Blue));

            Assert.That(authority.TryStart().Accepted, Is.True);
            Assert.That(authority.PendingChoice.OwningSide, Is.EqualTo(TeamSide.Red));
            Assert.That(authority.SelectedActor.IsDefined, Is.False);
            AssertRejected(
                authority.TryChooseTiedActor(TeamSide.Blue, first.Id),
                TurnResolutionRejectionReason.InitiativeCommandRejected);
            Assert.That(authority.PendingChoice, Is.Not.Null);

            Assert.That(authority.TryChooseTiedActor(TeamSide.Red, second.Id).Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(second.Id));
            Assert.That(authority.TryPerformHold(second.Id).Accepted, Is.True);

            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(first.Id));
        }

        [Test]
        public void RepeatedOrWrongHeroHold_IsRejectedWithoutAnotherActionOrMutation()
        {
            var fixture = CreateFixture(7, 4, TeamSide.Red);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var blueState = fixture.Blue.State;

            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Blue.Id),
                TurnResolutionRejectionReason.CardNotSelectedForAction);
            Assert.That(fixture.Blue.State, Is.EqualTo(blueState));
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));

            Assert.That(fixture.Authority.TryPerformHold(fixture.Red.Id).Accepted, Is.True);
            var redResolvedState = fixture.Red.State;
            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Red.Id),
                TurnResolutionRejectionReason.CardNotSelectedForAction);
            Assert.That(fixture.Red.State, Is.EqualTo(redResolvedState));
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void TurnOnlyEffectsCancellationCheckpoint_OccursOnlyWhenLastCardResolves()
        {
            var fixture = CreateFixture(7, 4, TeamSide.Blue);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);

            var firstHold = fixture.Authority.TryPerformHold(fixture.Red.Id);
            var secondHold = fixture.Authority.TryPerformHold(fixture.Blue.Id);

            Assert.That(firstHold.Events.Any(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint), Is.False);
            Assert.That(secondHold.Events.Count(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint), Is.EqualTo(1));
            Assert.That(secondHold.Events.Last().Kind, Is.EqualTo(TurnResolutionEventKind.TurnEnded));
        }

        [Test]
        public void HoldOffer_IsGeneralRuleSecondaryForAnyCurrentPlayedCardOnly()
        {
            var definition = CreateDefinition("offer", 5);
            var actorId = new HeroId("red-hero-1");
            var card = CreateRevealedCard("offer-card", definition, actorId.Value);

            var offer = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                card.Id,
                actorId,
                new[] { definition },
                new[] { card });

            Assert.That(offer.HasValue, Is.True);
            Assert.That(offer.Value.SourceCardInstanceId, Is.EqualTo(card.Id));
            Assert.That(offer.Value.SourceDefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(offer.Value.ActionDefinitionId,
                Is.EqualTo(GeneralRuleActionOfferQuery.HoldDefinition.Id));
            Assert.That(GeneralRuleActionOfferQuery.HoldDefinition.Kind, Is.EqualTo(ActionKind.Hold));
            Assert.That(GeneralRuleActionOfferQuery.HoldDefinition.Origin,
                Is.EqualTo(ActionDefinitionOrigin.GeneralRuleSecondary));
            Assert.That(GeneralRuleActionOfferQuery.HoldDefinition.DefaultPolicy.CardTextApplication,
                Is.EqualTo(CardTextApplicationPolicy.Suppressed));

            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
            Assert.That(GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                card.Id,
                actorId,
                new[] { definition },
                new[] { card }), Is.Null);
        }

        [Test]
        public void TurnEvents_RejectUndefinedSemanticPayloads()
        {
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.FromInitiative(default));
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.HoldPerformed(default));
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.CardWasResolved(default, default));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TurnResolutionEvent.TurnCheckpoint(TurnResolutionEventKind.CardResolved));
        }

        private static IEnumerable<InitiativeResolutionEventKind> EventKinds(
            TurnResolutionResult result)
        {
            return result.Events
                .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed)
                .Select(ruleEvent => ruleEvent.InitiativeEvent.Kind);
        }

        private static TestFixture CreateFixture(
            int redInitiative,
            int blueInitiative,
            TeamSide initialTieBreakerSide)
        {
            var participants = CreateParticipants(1, 1);
            var redDefinition = CreateDefinition("red", redInitiative);
            var blueDefinition = CreateDefinition("blue", blueInitiative);
            var red = CreateRevealedCard("red-card", redDefinition, "red-hero-1");
            var blue = CreateRevealedCard("blue-card", blueDefinition, "blue-hero-1");
            return new TestFixture(
                red,
                blue,
                new TurnResolutionAuthority(
                    new[] { redDefinition, blueDefinition },
                    new[] { blue, red },
                    participants,
                    new TieBreakerState(initialTieBreakerSide)));
        }

        private static CardDefinition CreateDefinition(string id, int initiative)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "turn-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture);
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
                heroes.Add(new Hero(new HeroId($"{prefix}-hero-{index}"), playerId));
            }
        }

        private static void AssertRejected(
            TurnResolutionResult result,
            TurnResolutionRejectionReason reason)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(reason));
            Assert.That(result.Events, Is.Empty);
        }

        private sealed class TestFixture
        {
            public TestFixture(
                CardInstance red,
                CardInstance blue,
                TurnResolutionAuthority authority)
            {
                Red = red;
                Blue = blue;
                Authority = authority;
            }

            public CardInstance Red { get; }

            public CardInstance Blue { get; }

            public TurnResolutionAuthority Authority { get; }
        }
    }
}
