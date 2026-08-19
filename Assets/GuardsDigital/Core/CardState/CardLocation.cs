namespace GuardsDigital.Core.Cards
{
    public enum CardLocation
    {
        None = 0,
        Hand = 1,
        TurnArea = 2,
        DiscardPile = 3,
        Deck = 4,
        SetAside = 5,
        ItemSlot = 6,

        /// <summary>
        /// Milestone 7.3. Rulebook p.16's place for an unlocked Ultimate: "Place it Faceup to the left
        /// side of your hero dashboard, facing the opposing team."
        ///
        /// It is a location of its own rather than a flag on the card, because the whole of what the
        /// unlock records is where the card now is. That is also what makes p.16's Ultimate distinct
        /// from an ordinary hand or deck card without anything having to say so: a card here is not in
        /// the hand a turn commits from, not in the deck an upgrade searches, and not in the discard
        /// pile end-of-round retrieval reads, so every one of those questions excludes it by asking
        /// where the card is.
        /// </summary>
        UltimateArea = 7
    }
}
