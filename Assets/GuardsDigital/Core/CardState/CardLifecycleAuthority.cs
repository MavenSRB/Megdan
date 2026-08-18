using System;

namespace GuardsDigital.Core.Cards
{
    public static class CardLifecycleAuthority
    {
        public static bool TryPlayFacedown(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!card.State.Equals(CardStateFacts.InitialInHand()))
            {
                return false;
            }

            card.ApplyState(new CardStateFacts(
                CardLocation.TurnArea,
                true,
                CardResolutionStatus.Unresolved,
                CardFaceOrientation.FaceDown));
            return true;
        }

        public static bool TryReveal(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.State.Location != CardLocation.TurnArea ||
                !card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.Unresolved ||
                card.State.FaceOrientation != CardFaceOrientation.FaceDown)
            {
                return false;
            }

            card.ApplyState(new CardStateFacts(
                card.State.Location,
                card.State.HasBeenPlayed,
                card.State.ResolutionStatus,
                CardFaceOrientation.FaceUp));
            return true;
        }

        public static bool TryResolveAfterAction(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.State.Location != CardLocation.TurnArea ||
                !card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.Unresolved ||
                card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return false;
            }

            card.ApplyState(new CardStateFacts(
                card.State.Location,
                card.State.HasBeenPlayed,
                CardResolutionStatus.Resolved,
                card.State.FaceOrientation));
            return true;
        }

        public static bool TryDiscardDefenseFromHand(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.State.Location != CardLocation.Hand ||
                card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.None)
            {
                return false;
            }

            return card.TryApplyLocationTransition(
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
        }

        /// <summary>
        /// Rulebook p.8's Retrieve Cards, asked of one card: is this card one the round returns to its
        /// owner's hand, and if so return it in fresh state.
        ///
        /// Two shapes are eligible and they are the two the round can produce - a played card that
        /// resolved, and a Defense discarded from hand without being played. Everything else is
        /// ineligible by not being either: a card still in hand, one set aside, one in an item slot.
        /// Face orientation is deliberately not part of either shape, because it is orthogonal to both:
        /// a played card is revealed before it can resolve, so no resolved card is facedown, and adding
        /// the redundant condition would let a future facedown resolution silently stop being retrieved.
        ///
        /// Milestone 6.13 renamed this from `TryRetrieveEligibleSyntheticCard` and changed nothing else.
        /// It is the only eligibility question end-of-round retrieval asks, and the milestone's audit
        /// forbids a second one existing beside it.
        /// </summary>
        public static bool TryRetrieveEligibleCard(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var isResolvedPlayedCard =
                card.State.Location == CardLocation.TurnArea &&
                card.State.HasBeenPlayed &&
                card.State.ResolutionStatus == CardResolutionStatus.Resolved;
            var isDiscardedDefense =
                card.State.Location == CardLocation.DiscardPile &&
                !card.State.HasBeenPlayed &&
                card.State.ResolutionStatus == CardResolutionStatus.None;
            if (!isResolvedPlayedCard && !isDiscardedDefense)
            {
                return false;
            }

            card.ApplyState(CardStateFacts.InitialInHand());
            return true;
        }
    }
}
