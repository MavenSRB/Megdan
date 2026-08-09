using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct PrintedValueDefinition : IEquatable<PrintedValueDefinition>
    {
        public PrintedValueDefinition(PrintedValueChannel channel, int amount)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("Printed value requires a channel.", nameof(channel));
            }

            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Printed value amount must not be negative.");
            }

            Channel = channel;
            Amount = amount;
        }

        public PrintedValueChannel Channel { get; }

        public int Amount { get; }

        public bool Equals(PrintedValueDefinition other)
        {
            return Channel == other.Channel && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is PrintedValueDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Channel * 397) ^ Amount;
            }
        }
    }
}
