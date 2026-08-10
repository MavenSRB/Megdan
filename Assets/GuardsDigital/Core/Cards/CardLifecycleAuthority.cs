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

        public static bool TryRetrieveEligibleSyntheticCard(CardInstance card)
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
