using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct InitiativeValue : IEquatable<InitiativeValue>, IComparable<InitiativeValue>
    {
        public InitiativeValue(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Initiative must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool IsDefined => Value > 0;

        public int CompareTo(InitiativeValue other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(InitiativeValue other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is InitiativeValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
