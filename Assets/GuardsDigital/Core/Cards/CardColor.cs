namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// A card's printed colour, as a typed rules-facing characteristic.
    ///
    /// The five members are the five the rulebook uses. Page 7 states the relationship that fixes the
    /// set - "Primary Action Icons (Always match the card's color)" over the five primary icons - and
    /// page 15 names two of them as the Basic pair: "Your Basic cards (Gold and Silver), are never
    /// upgraded, have no Tier". Page 15 also states the count from the other side: after leveling "You
    /// should now have exactly 5 cards, 1 of each color, in your hand", and page 6's setup deals that
    /// hand as "the three Tier I cards and the two basic cards with no Tier".
    ///
    /// <see cref="Red"/> and <see cref="Blue"/> are card colours and have nothing to do with
    /// <c>TeamSide.Red</c> and <c>TeamSide.Blue</c>. Nothing may convert between the two types, and the
    /// synthetic content deliberately gives each team a card of each colour so that no derivation from
    /// team identity could reproduce the assignment. A colour is likewise never inferred from artwork,
    /// tint, display name, hero identity, printed action icon, hand position, or UI grouping: every
    /// definition states its own through <see cref="CardProgressionProfile"/>, which
    /// <see cref="CardDefinition"/> refuses to be constructed without.
    ///
    /// <see cref="None"/> is not a sixth colour and no definition may declare it as one. It exists so
    /// that <see cref="CardProgressionProfile.NotStated"/> - a definition that states no progression
    /// content at all - has a value to carry, and the profile is what makes that absence explicit
    /// rather than defaulted.
    /// </summary>
    public enum CardColor
    {
        None = 0,
        Gold = 1,
        Silver = 2,
        Red = 3,
        Green = 4,
        Blue = 5
    }
}
