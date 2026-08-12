using System;

namespace GuardsDigital.Core.Timing
{
    public readonly struct ActiveEffectId : IEquatable<ActiveEffectId>, IComparable<ActiveEffectId>
    {
        public ActiveEffectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An Active-effect ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public int CompareTo(ActiveEffectId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public bool Equals(ActiveEffectId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActiveEffectId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }
}
