using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>A read-only live delta from an immutable printed icon, not a replacement rule.</summary>
    public sealed class CardValuePreview
    {
        public CardValuePreview(HeroId owner, CardDefinitionReference definition,
            PrintedValueChannel channel, int bonus)
        {
            if (!owner.IsDefined || !definition.IsDefined)
                throw new ArgumentException("A card value names its owner and versioned face.");
            if (!Enum.IsDefined(typeof(PrintedValueChannel), channel) ||
                channel == PrintedValueChannel.None)
                throw new ArgumentOutOfRangeException(nameof(channel));
            Owner = owner;
            Definition = definition;
            Channel = channel;
            Bonus = bonus;
        }

        public HeroId Owner { get; }
        public CardDefinitionReference Definition { get; }
        public PrintedValueChannel Channel { get; }
        public int Bonus { get; }
    }
}
