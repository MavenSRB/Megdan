using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AwaitedResponderNarrationTests
    {
        [Test]
        public void EveryDeclaredPendingFamilyHasAnExplicitPublicActivity()
        {
            var families = typeof(PendingDecisionFamilies).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(PendingDecisionFamilyId)).ToArray();
            Assert.That(families, Is.Not.Empty);
            foreach (var field in families)
                Assert.That(NarrationLabels.PublicDecisionActivity((PendingDecisionFamilyId)field.GetValue(null)),
                    Is.Not.Empty.And.Not.EqualTo("CHOOSING A RESPONSE"), field.Name);
        }

        [Test]
        public void DefenseWaitNamesTheResponderWithoutExposingTheirHandAndReturnsToTheAttacker()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var host = new MatchHost(session);
            var choice = session.Projection.PendingDefenseChoice;
            var attacker = session.Projection.SelectedActor.ActorId;
            var defender = choice.ChooserId;
            var snapshot = session.CaptureSnapshot();
            var observer = Projection(host, attacker);
            var expected = Name(host, defender) + " — CHOOSING A DEFENSE CARD";

            Assert.That(RoundActionPresenter.CreateViewModel(observer, attacker).Status, Is.EqualTo(expected));
            Assert.That(observer.PendingDefenseChoice, Is.Null);
            Assert.That(observer.SelectedActor.ActorId, Is.EqualTo(attacker), "The responder is not the turn's actor.");
            Assert.That(choice.LegalDefenseOffers.All(offer => observer.Cards.All(card =>
                !card.Id.Equals(offer.SourceCardInstanceId))), Is.True);
            var own = RoundActionPresenter.CreateViewModel(Projection(host, defender), defender);
            Assert.That(own.Status, Is.EqualTo("YOUR DEFENSE — CHOOSE A CARD, OR DECLINE"));
            Assert.That(own.DefenseChoices, Is.Not.Empty);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(snapshot), "Narration has no authoritative state.");
            Assert.That(session.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(RoundActionPresenter.CreateViewModel(Projection(host, attacker), attacker).Status, Is.EqualTo(expected));

            var seat = host.Seats.Seats.Single(value => value.HeroId.Equals(defender));
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
            var response = client.SeatProjection.AuthorizedCommands.First(command => command.Kind == DirectorCommandKind.ChooseDefenseCard);
            var result = client.Submit(response);
            Assert.That(result.Accepted, Is.True, result.Detail);
            var resumed = RoundActionPresenter.CreateViewModel(Projection(host, attacker), attacker);
            Assert.That(resumed.Status, Does.StartWith("YOUR TURN"));
            Assert.That(resumed.Status, Does.Not.Contain("CHOOSING A DEFENSE CARD"));
        }

        [Test]
        public void AnOpponentsDiscardChoiceNamesItsOwnerButNotItsCandidatesOrSelection()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minion);
            CardInstance Hand(TeamSide side, CardDefinitionId id) => session.Projection.Cards.Single(card =>
                card.State.Location == CardLocation.Hand && card.DefinitionReference.DefinitionId.Equals(id) &&
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side);
            var red = Hand(TeamSide.Red, WaspCardContent.Shock);
            var blue = Hand(TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var attack = session.Projection.AvailableTurnActions.Single(offer => session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference)).ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseAttackTarget(target.ResolutionId, target.ChoiceId, minion));
            var heroChoice = session.Projection.PendingEffectChoice;
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(heroChoice.SelectInvocations
                .Single(invocation => invocation.Candidate.Value.HeroId.Equals(blue.OwnerId))));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(session.Projection.PendingEffectChoice.ConfirmInvocation));

            var host = new MatchHost(session);
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.ChooserId, Is.EqualTo(blue.OwnerId));
            var observer = Projection(host, red.OwnerId);
            var status = RoundActionPresenter.CreateViewModel(observer, red.OwnerId).Status;
            Assert.That(status, Is.EqualTo(Name(host, blue.OwnerId) + " — RESOLVING A CARD EFFECT"));
            Assert.That(observer.PendingEffectChoice, Is.Null);
            Assert.That(observer.DecisionContract, Is.Null);
            Assert.That(choice.Candidates.All(candidate => observer.Cards.All(card =>
                !card.Id.Equals(candidate.Value.CardInstanceId))), Is.True);
            Assert.That(RoundActionPresenter.CreateViewModel(Projection(host, blue.OwnerId), blue.OwnerId).Status,
                Does.StartWith("YOUR CARD CHOICE").And.Contain("to discard"));
        }

        private static SeatProjection Projection(MatchHost host, HeroId hero) =>
            host.ProjectionForSeat(host.Seats.Seats.Single(seat => seat.HeroId.Equals(hero)).Id);

        private static string Name(MatchHost host, HeroId hero) =>
            host.Projection.Setup.Heroes.Single(value => value.HeroId.Equals(hero)).DisplayName.ToUpperInvariant();
    }
}
