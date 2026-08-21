namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// How a printed Defense action resolves against an incoming attack - rulebook p.14's two shapes.
    ///
    /// Milestone 8.3, added with the first verified content that needs the second one. The secondary
    /// Defense icon every card has carried so far prints a number, and p.14 compares it: "if the
    /// Defense Total is equal to or higher than the Attack Total, the attack is defended". A Block card
    /// prints a shield with an exclamation mark and no number at all, and p.14 resolves it by a
    /// condition instead: the block succeeds regardless of the attack value when its condition is met,
    /// and the hero is defeated when it is not.
    ///
    /// The distinction is declared rather than derived from an absent value, because "prints no Defense
    /// number" is also what a malformed definition looks like. It is likewise not expressed as a
    /// sentinel amount - a Block written as Defense 99 would win a comparison it is never supposed to
    /// enter, and milestone 8.11 is where the condition itself gets evaluated on the
    /// <c>DefenseBlockResult</c> seam milestone 5.8 reserved.
    /// </summary>
    public enum PrintedDefenseResolution
    {
        /// <summary>Not a Defense action.</summary>
        None = 0,

        /// <summary>p.14's numeric Defense: the printed value enters the Defense Total comparison.</summary>
        Value = 1,

        /// <summary>
        /// p.14's Block card: no printed Defense value, a condition read from the card's text, and no
        /// comparison. Milestone 8.11 evaluates it; a definition may declare it before then, which is
        /// what lets Wasp's three Green cards carry their printed faces honestly.
        /// </summary>
        Block = 2
    }
}
