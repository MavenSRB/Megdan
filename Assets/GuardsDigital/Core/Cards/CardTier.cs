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
    /// <see cref="IV"/> is the Ultimate's, added by milestone 7.3 with the card that declares it. p.16
    /// says it outright - "The Ultimate is a special Tier IV card" - and p.15's Hero Level table agrees
    /// from the other side, ending its Max card Tier row at IV. It is reached by no upgrade: p.15's
    /// Upgrade a Card step looks for cards "one Tier higher", and p.16 replaces that step entirely for
    /// the level that unlocks the Ultimate, so no Tier III card names a Tier IV candidate and
    /// <see cref="CardProgressionValidator"/> refuses one that tries.
    ///
    /// The set stops there. The remaining printed value in the source is the Handicap card's "H" Tier,
    /// which p.6 returns to the box during setup and which therefore never enters a match; adding a
    /// member for it would put a value into the type that no definition declares and no query answers
    /// for.
    /// </summary>
    public enum CardTier
    {
        None = 0,
        I = 1,
        II = 2,
        III = 3,

        /// <summary>p.16's Ultimate: "a special Tier IV card", unlocked rather than upgraded into.</summary>
        IV = 4
    }
}
