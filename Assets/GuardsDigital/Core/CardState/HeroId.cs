using System;

namespace GuardsDigital.Core.Units.Heroes
{
    /// <summary>
    /// Extracted from HeroOccupancy.cs at milestone 6.1f. <see cref="CardInstance"/> carries an owner
    /// identity, so this type has to live in the card-state assembly for that assembly to sit below
    /// the rest of Core. HeroOccupancy stays behind because it also carries a board coordinate, and
    /// dragging the board graph down here would defeat the point of the split.
    ///
    /// The namespace is deliberately unchanged, so nothing that already names
    /// GuardsDigital.Core.Units.Heroes.HeroId has to move with it.
    /// </summary>
    public readonly struct HeroId : IEquatable<HeroId>
    {
        public HeroId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Hero ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(HeroId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is HeroId other && Equals(other);
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
