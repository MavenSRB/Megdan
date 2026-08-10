using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardLifecycleAuthorityTests
    {
        [Test]
        public void PlayedCard_TransitionsFacedownThenRevealsAndResolvesAfterAction()
        {
            var card = CreateInHandCard("played-card");

            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(card.State, Is.EqualTo(new CardStateFacts(
                CardLocation.TurnArea,
                true,
                CardResolutionStatus.Unresolved,
                CardFaceOrientation.FaceDown)));

            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            Assert.That(card.State, Is.EqualTo(new CardStateFacts(
                CardLocation.TurnArea,
                true,
                CardResolutionStatus.Unresolved,
                CardFaceOrientation.FaceUp)));

            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
            Assert.That(card.State, Is.EqualTo(new CardStateFacts(
                CardLocation.TurnArea,
                true,
                CardResolutionStatus.Resolved,
                CardFaceOrientation.FaceUp)));
        }

        [Test]
        public void LifecycleTransitions_RejectOutOfOrderAttemptsWithoutMutation()
        {
            var card = CreateInHandCard("out-of-order-card");
            var initialState = card.State;

            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.False);
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.False);
            Assert.That(card.State, Is.EqualTo(initialState));

            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            var facedownState = card.State;
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.False);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.False);
            Assert.That(card.State, Is.EqualTo(facedownState));
        }

        [Test]
        public void DefenseCard_DiscardsFromHandWithoutBecomingPlayedOrResolved()
        {
            var card = CreateInHandCard("defense-card");

            Assert.That(CardLifecycleAuthority.TryDiscardDefenseFromHand(card), Is.True);
            Assert.That(card.State, Is.EqualTo(CardStateFacts.DiscardedUnplayed()));
            Assert.That(CardLifecycleAuthority.TryDiscardDefenseFromHand(card), Is.False);
            Assert.That(card.State, Is.EqualTo(CardStateFacts.DiscardedUnplayed()));
        }

        [Test]
        public void ResolvedDiscardedAndFaceOrientation_RemainOrthogonalFacts()
        {
            var state = new CardStateFacts(
                CardLocation.DiscardPile,
                true,
                CardResolutionStatus.Resolved,
                CardFaceOrientation.FaceDown);

            Assert.That(state.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(state.HasBeenPlayed, Is.True);
            Assert.That(state.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(state.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
        }

        [Test]
        public void LifecycleAuthority_RejectsMissingCard()
        {
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryPlayFacedown(null));
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryReveal(null));
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryResolveAfterAction(null));
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryDiscardDefenseFromHand(null));
        }

        private static CardInstance CreateInHandCard(string instanceId)
        {
            return CardInstance.CreateInHand(
                new CardInstanceId(instanceId),
                new CardDefinitionReference(new CardDefinitionId("synthetic-lifecycle-card"), "rev-1"),
                new HeroId("synthetic-hero"));
        }
    }
}
