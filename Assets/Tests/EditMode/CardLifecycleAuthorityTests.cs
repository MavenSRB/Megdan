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
        public void Retrieval_ReturnsOnlyResolvedPlayedOrDiscardedDefenseCardsToFreshHandState()
        {
            var definition = new CardDefinitionReference(new CardDefinitionId("retrieval"), "rev-1");
            var owner = new GuardsDigital.Core.Units.Heroes.HeroId("retrieval-owner");
            var resolved = CardInstance.CreateInHand(new CardInstanceId("resolved"), definition, owner);
            var discarded = CardInstance.CreateInHand(new CardInstanceId("discarded"), definition, owner);
            var untouched = CardInstance.CreateInHand(new CardInstanceId("untouched"), definition, owner);
            var setAside = new CardInstance(
                new CardInstanceId("set-aside"),
                definition,
                owner,
                new CardStateFacts(
                    CardLocation.SetAside,
                    false,
                    CardResolutionStatus.None,
                    CardFaceOrientation.FaceUp));
            var item = new CardInstance(
                new CardInstanceId("item"),
                definition,
                owner,
                new CardStateFacts(
                    CardLocation.ItemSlot,
                    false,
                    CardResolutionStatus.None,
                    CardFaceOrientation.FaceUp));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(resolved), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(resolved), Is.True);
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(resolved), Is.True);
            Assert.That(CardLifecycleAuthority.TryDiscardDefenseFromHand(discarded), Is.True);

            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(resolved), Is.True);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(discarded), Is.True);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(untouched), Is.False);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(setAside), Is.False);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(item), Is.False);
            Assert.That(resolved.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(discarded.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(untouched.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(setAside.State.Location, Is.EqualTo(CardLocation.SetAside));
            Assert.That(item.State.Location, Is.EqualTo(CardLocation.ItemSlot));
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(resolved), Is.False);
            Assert.Throws<ArgumentNullException>(() =>
                CardLifecycleAuthority.TryRetrieveEligibleSyntheticCard(null));
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
