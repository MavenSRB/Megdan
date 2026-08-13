using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct ActionCardStateTransition : IEquatable<ActionCardStateTransition>
    {
        public ActionCardStateTransition(CardLocation requiredLocation, CardLocation destinationLocation)
        {
            if (!Enum.IsDefined(typeof(CardLocation), requiredLocation) || requiredLocation == CardLocation.None)
            {
                throw new ArgumentException("An action card-state transition requires a source location.", nameof(requiredLocation));
            }

            if (!Enum.IsDefined(typeof(CardLocation), destinationLocation) || destinationLocation == CardLocation.None)
            {
                throw new ArgumentException("An action card-state transition requires a destination location.", nameof(destinationLocation));
            }

            if (requiredLocation == destinationLocation)
            {
                throw new ArgumentException("An action card-state transition must change the card location.", nameof(destinationLocation));
            }

            RequiredLocation = requiredLocation;
            DestinationLocation = destinationLocation;
        }

        public CardLocation RequiredLocation { get; }

        public CardLocation DestinationLocation { get; }

        public bool IsDefined => RequiredLocation != CardLocation.None && DestinationLocation != CardLocation.None;

        public bool Equals(ActionCardStateTransition other)
        {
            return RequiredLocation == other.RequiredLocation && DestinationLocation == other.DestinationLocation;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionCardStateTransition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)RequiredLocation * 397) ^ (int)DestinationLocation;
            }
        }
    }
}
