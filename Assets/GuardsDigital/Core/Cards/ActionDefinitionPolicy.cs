using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct ActionDefinitionPolicy : IEquatable<ActionDefinitionPolicy>
    {
        public ActionDefinitionPolicy(
            CardTextApplicationPolicy cardTextApplication,
            bool canRepeat,
            bool canBeReplaced)
        {
            if (cardTextApplication == CardTextApplicationPolicy.None)
            {
                throw new ArgumentException("Action definition policy requires a card-text application policy.", nameof(cardTextApplication));
            }

            CardTextApplication = cardTextApplication;
            CanRepeat = canRepeat;
            CanBeReplaced = canBeReplaced;
        }

        public CardTextApplicationPolicy CardTextApplication { get; }

        public bool CanRepeat { get; }

        public bool CanBeReplaced { get; }

        public bool Equals(ActionDefinitionPolicy other)
        {
            return CardTextApplication == other.CardTextApplication &&
                CanRepeat == other.CanRepeat &&
                CanBeReplaced == other.CanBeReplaced;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionDefinitionPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)CardTextApplication;
                hashCode = (hashCode * 397) ^ CanRepeat.GetHashCode();
                hashCode = (hashCode * 397) ^ CanBeReplaced.GetHashCode();
                return hashCode;
            }
        }
    }
}
