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

            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
            Assert.That(card.State, Is.EqualTo(CardStateFacts.DiscardedUnplayed()));
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.False);
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
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(discarded), Is.True);

            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(resolved), Is.True);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(discarded), Is.True);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(untouched), Is.False);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(setAside), Is.False);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(item), Is.False);
            Assert.That(resolved.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(discarded.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(untouched.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(setAside.State.Location, Is.EqualTo(CardLocation.SetAside));
            Assert.That(item.State.Location, Is.EqualTo(CardLocation.ItemSlot));
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(resolved), Is.False);
            Assert.Throws<ArgumentNullException>(() =>
                CardLifecycleAuthority.TryRetrieveEligibleCard(null));
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

        /// <summary>
        /// Milestone 6.13's eligibility audit, stated as the negative it settles: face orientation is
        /// orthogonal to both admitted shapes, so it cannot by itself make a card eligible or
        /// ineligible. A resolved played card is retrieved whichever way it is facing, and a facedown
        /// card that never resolved is not retrieved for being facedown - it is ineligible for being
        /// unresolved, which is a different fact about a different field.
        /// </summary>
        [Test]
        public void Retrieval_ReadsTheTwoAdmittedShapesWithoutReadingFaceOrientation()
        {
            var definition = new CardDefinitionReference(new CardDefinitionId("retrieval"), "rev-1");
            var owner = new HeroId("retrieval-owner");
            var faceDownResolved = new CardInstance(
                new CardInstanceId("face-down-resolved"),
                definition,
                owner,
                new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceDown));
            var faceUpUnresolved = new CardInstance(
                new CardInstanceId("face-up-unresolved"),
                definition,
                owner,
                new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceUp));

            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(faceDownResolved), Is.True,
                "The resolved-played shape names location, played and resolution, and nothing else.");
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(faceUpUnresolved), Is.False,
                "and an unresolved card is refused for being unresolved rather than for its face.");
            Assert.That(faceDownResolved.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(faceUpUnresolved.State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void LifecycleAuthority_RejectsMissingCard()
        {
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryPlayFacedown(null));
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryReveal(null));
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryResolveAfterAction(null));
            Assert.Throws<ArgumentNullException>(() => CardLifecycleAuthority.TryDiscardFromHand(null));
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
