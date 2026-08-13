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

        public static CardStateFacts DiscardedUnplayed()
        {
            return new CardStateFacts(
                CardLocation.DiscardPile,
                false,
                CardResolutionStatus.None,
                CardFaceOrientation.FaceUp);
        }

        public bool IsInitialInHand => Equals(InitialInHand());

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
