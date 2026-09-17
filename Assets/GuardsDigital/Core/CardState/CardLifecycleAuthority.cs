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

        /// <summary>
        /// The card-system contract's <c>DiscardCard</c>: move one named in-hand card to the discard
        /// pile without playing it.
        ///
        /// It decides one thing and states what it refuses to decide. The only question it asks is
        /// whether this card is in the lifecycle shape a discard from hand starts in - in hand,
        /// unplayed, with no resolution status. It does not ask whose card it is, what colour it is,
        /// whether the card is a Defense, or who chose it, because those belong to whatever effect or
        /// reaction reached this point: milestone 7.1's acceptance check puts it exactly that way, and
        /// a mutation that re-derived eligibility would give a colour-restricted discard two different
        /// candidate sets - the query's and its own - that nothing keeps in agreement.
        ///
        /// Milestone 7.1 renamed this from <c>TryDiscardDefenseFromHand</c> and changed nothing else.
        /// Its behaviour was never Defense-specific; the Defense reaction is one caller that has
        /// already decided the card is a legal Defense before it arrives here, and a colour-restricted
        /// discard is another that has already decided the colour. A second, general discard beside a
        /// Defense-named one would have been two names for one transition, which is the shape milestone
        /// 6.13's audit removed from retrieval.
        /// </summary>
        public static bool TryDiscardFromHand(CardInstance card)
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

        /// <summary>p.10: discarding a resolved card changes its location, not its played history.</summary>
        public static bool TryDiscardResolvedCard(CardInstance card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (!IsResolvedCardInTurnArea(card)) return false;
            card.ApplyState(new CardStateFacts(
                CardLocation.DiscardPile, true, CardResolutionStatus.Resolved,
                card.State.FaceOrientation));
            return true;
        }

        public static bool IsResolvedCardInTurnArea(CardInstance card) =>
            card != null && card.State.Location == CardLocation.TurnArea &&
            card.State.HasBeenPlayed && card.State.ResolutionStatus == CardResolutionStatus.Resolved;

        /// <summary>The two legal discarded shapes; an unresolved played discard is not supported.</summary>
        public static bool IsDiscardedRestingCard(CardInstance card) =>
            card != null && card.State.Location == CardLocation.DiscardPile &&
            ((!card.State.HasBeenPlayed && card.State.ResolutionStatus == CardResolutionStatus.None) ||
             (card.State.HasBeenPlayed && card.State.ResolutionStatus == CardResolutionStatus.Resolved));

        /// <summary>
        /// p.15's "Return this card to your deck", the first of the three transitions an upgrade moves
        /// cards through.
        ///
        /// Milestone 7.1 supplies the transitions and not the purchase: nothing here spends a coin,
        /// checks a Level, picks which card is returned, or orders anything, all of which are milestone
        /// 7.5's. What it does prove is that the instance survives the move - the same instance keeps
        /// its owner and its definition reference, because only <see cref="CardInstance.State"/> moves
        /// and the identity fields are immutable - which is what acceptance check six is about.
        /// </summary>
        public static bool TryReturnToDeck(CardInstance card) =>
            TryMove(card, CardLocation.Hand, CardLocation.Deck, CardFaceOrientation.FaceUp);

        /// <summary>
        /// p.15's "Take one of those cards into your hand". The card is one the player found in their
        /// own deck, so it arrives in the ordinary in-hand shape a turn can commit.
        /// </summary>
        public static bool TryTakeFromDeckIntoHand(CardInstance card) =>
            TryMove(card, CardLocation.Deck, CardLocation.Hand, CardFaceOrientation.FaceUp);

        /// <summary>
        /// p.15's "set the other one aside, Facedown, next to your hero dashboard - you will need it
        /// later". Facedown is the page's own word and is kept, so the card's colour and Tier stop
        /// being rules-facing information while it waits; p.16 turns it Faceup when it becomes an item,
        /// which is milestone 7.2's transition rather than this one.
        /// </summary>
        public static bool TrySetAsideFromDeck(CardInstance card) =>
            TryMove(card, CardLocation.Deck, CardLocation.SetAside, CardFaceOrientation.FaceDown);

        /// <summary>
        /// p.16's "tuck it Faceup under your hero dashboard, under the slot with a matching icon" - the
        /// transition milestone 7.1 named and left to this one.
        ///
        /// Faceup is the page's own word and is the whole of the orientation change: the card that waited
        /// Facedown beside the dashboard becomes rules-facing again, which the page requires from the
        /// other side too ("Your items must be clearly visible to the opposing team"). What this decides
        /// is only the lifecycle shape, exactly as the three transitions above do. Which slot the card
        /// goes under, whether it prints an Item icon at all, and where it falls in the hero's sequence
        /// are <c>HeroItemState.TryPlaceSetAsideCard</c>'s questions, and a mutation that re-derived them
        /// would give the placement two answers that nothing keeps in agreement - the same division of
        /// labour milestone 7.1 drew between <see cref="TryDiscardFromHand"/> and the colour-restricted
        /// discard that chooses what it discards.
        /// </summary>
        public static bool TryPlaceAsItemFromSetAside(CardInstance card) =>
            TryMove(card, CardLocation.SetAside, CardLocation.ItemSlot, CardFaceOrientation.FaceUp);

        /// <summary>
        /// p.16's "Place it Faceup to the left side of your hero dashboard, facing the opposing team" -
        /// milestone 7.3's transition, and the last resting place a card in this build can reach.
        ///
        /// It comes from the deck because that is where p.6's setup leaves it: the player takes their
        /// five starting cards and returns "the remaining Tier II, Tier III, and Tier IV cards to your
        /// deck". Faceup is the page's own word, and it matters for the same reason it does for an
        /// item - the card's Tier and classification become rules-facing at the moment it is placed,
        /// which is what lets the unlocked Ultimate be found by asking about the card rather than by
        /// consulting a second record.
        ///
        /// This decides the lifecycle shape and nothing else. Whether this hero may unlock yet, whether
        /// this is that hero's own Ultimate, and whether one is already unlocked are
        /// <c>UltimateUnlockQuery</c>'s questions, exactly as the slot is <c>HeroItemState</c>'s and the
        /// colour is the colour-restricted discard's.
        /// </summary>
        public static bool TryUnlockUltimateFromDeck(CardInstance card) =>
            TryMove(card, CardLocation.Deck, CardLocation.UltimateArea, CardFaceOrientation.FaceUp);

        /// <summary>
        /// One unplayed card moving between two resting places. Every caller above is a rulebook
        /// sentence; this is the single shape all three share, so none of them can accidentally admit a
        /// played or resolved card.
        /// </summary>
        private static bool TryMove(
            CardInstance card,
            CardLocation from,
            CardLocation to,
            CardFaceOrientation faceOrientation)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.State.Location != from ||
                card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.None)
            {
                return false;
            }

            card.ApplyState(new CardStateFacts(to, false, CardResolutionStatus.None, faceOrientation));
            return true;
        }

        /// <summary>
        /// Rulebook p.8's Retrieve Cards, asked of one card: is this card one the round returns to its
        /// owner's hand, and if so return it in fresh state.
        ///
        /// Resolved played cards are eligible in the turn area or discard pile, as are unplayed
        /// hand-discards. Hand, deck, item and unresolved states are ineligible. Face orientation
        /// is orthogonal: either face of each eligible resting state returns to a fresh hand.
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

            if (!IsResolvedCardInTurnArea(card) && !IsDiscardedRestingCard(card))
            {
                return false;
            }

            card.ApplyState(CardStateFacts.InitialInHand());
            return true;
        }
    }
}
