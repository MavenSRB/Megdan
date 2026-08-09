using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct CardDefinitionId : IEquatable<CardDefinitionId>
    {
        public CardDefinitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Card definition ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CardDefinitionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CardDefinitionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
