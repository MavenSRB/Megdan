using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct CardStateFacts : IEquatable<CardStateFacts>
    {
        public CardStateFacts(
            CardLocation location,
            bool hasBeenPlayed,
            CardResolutionStatus resolutionStatus,
            CardFaceOrientation faceOrientation)
        {
            if (location == CardLocation.None)
            {
                throw new ArgumentException("Card state requires a location.", nameof(location));
            }

            if (faceOrientation == CardFaceOrientation.None)
            {
                throw new ArgumentException("Card state requires a face orientation.", nameof(faceOrientation));
            }

            if (!hasBeenPlayed && resolutionStatus != CardResolutionStatus.None)
            {
                throw new ArgumentException("Only a played card may have a resolution status.", nameof(resolutionStatus));
            }

            Location = location;
            HasBeenPlayed = hasBeenPlayed;
            ResolutionStatus = resolutionStatus;
            FaceOrientation = faceOrientation;
        }

        public CardLocation Location { get; }

        public bool HasBeenPlayed { get; }

        public CardResolutionStatus ResolutionStatus { get; }

        public CardFaceOrientation FaceOrientation { get; }

        public static CardStateFacts InitialInHand()
        {
            return new CardStateFacts(
                CardLocation.Hand,
                false,
                CardResolutionStatus.None,
                CardFaceOrientation.FaceUp);
        }

        /// <summary>
        /// Milestone 7.5. The shape a card starts a match in when rulebook p.6 leaves it in the deck
        /// rather than dealing it: "return the remaining Tier II, Tier III, and Tier IV cards to your
        /// deck."
        ///
        /// Face-up, matching the state <c>CardLifecycleAuthority.TryReturnToDeck</c> gives a card p.15
        /// returns to the same deck. The two have to agree, or a card would mean something different
        /// depending on whether it started there or arrived there, and p.15 sends the player to search
        /// the deck by colour and Tier - which a face-down card's suppressed printed characteristics
        /// would make unanswerable.
        /// </summary>
        public static CardStateFacts InitialInDeck()
        {
            return new CardStateFacts(
                CardLocation.Deck,
                false,
                CardResolutionStatus.None,
                CardFaceOrientation.FaceUp);
        }

        public static CardStateFacts DiscardedUnplayed()
        {
            return new CardStateFacts(
                CardLocation.DiscardPile,
                false,
                CardResolutionStatus.None,
                CardFaceOrientation.FaceUp);
        }

        public bool IsInitialInHand => Equals(InitialInHand());

        /// <summary>Milestone 7.5: whether this is exactly the shape <see cref="InitialInDeck"/> states.</summary>
        public bool IsInitialInDeck => Equals(InitialInDeck());

        public bool Equals(CardStateFacts other)
        {
            return Location == other.Location &&
                HasBeenPlayed == other.HasBeenPlayed &&
                ResolutionStatus == other.ResolutionStatus &&
                FaceOrientation == other.FaceOrientation;
        }

        public override bool Equals(object obj)
        {
            return obj is CardStateFacts other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Location;
                hashCode = (hashCode * 397) ^ HasBeenPlayed.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ResolutionStatus;
                hashCode = (hashCode * 397) ^ (int)FaceOrientation;
                return hashCode;
            }
        }
    }
}
