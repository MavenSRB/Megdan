namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// A card's printed Tier - rulebook p.6's "Roman numeral in the top right corner of each Action
    /// card is its Tier".
    ///
    /// <see cref="None"/> is a positive fact rather than a missing value: p.15 states that the Basic
    /// pair "are never upgraded, have no Tier, and are not considered when checking your lowest card
    /// Tier". <see cref="CardProgressionProfile"/> is what keeps the two readings apart - a Basic card
    /// declares <see cref="None"/> and is refused any next-Tier candidate, and a non-Basic card is
    /// refused <see cref="None"/> outright.
    ///
    /// The set stops at <see cref="III"/> deliberately. Two further printed values exist in the source
    /// and neither belongs to this milestone: Tier IV is the Ultimate, which p.16 describes as "a
    /// special Tier IV card" and which milestone 7.3 owns, and the Handicap card's "H" Tier is returned
    /// to the box during setup by p.6 and so never enters a match. Adding either member here would put
    /// a value into the type that no definition declares and no query answers for.
    /// </summary>
    public enum CardTier
    {
        None = 0,
        I = 1,
        II = 2,
        III = 3
    }
}
