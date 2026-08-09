using System;

namespace GuardsDigital.Core.Obstacles
{
    public readonly struct ObstacleId : IEquatable<ObstacleId>
    {
        public ObstacleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Obstacle ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ObstacleId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObstacleId other && Equals(other);
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
