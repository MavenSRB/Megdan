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
        UltimateArea = 7,

        /// <summary>
        /// Milestone 9.30b. A hero's spellbook: the spell cards that hero owns outside the hand, deck
        /// and discard pile, lying facedown. Gydion's Prepare Spells: "Add all spell cards to the
        /// spellbook. Only Gydion can look at the cards in the spellbook."
        ///
        /// <para>A location of its own for the reason <see cref="UltimateArea"/> is one: every
        /// question p.10, p.15 and p.8 ask names its location, so a card here is not in the hand a
        /// turn commits from, the deck an upgrade searches or the discard pile retrieval reads,
        /// without any of them having to say so. Facedown is the card's own orientation, so the
        /// projection's fail-closed rule withholds its face from every seat but its owner's.</para>
        ///
        /// <para>Appended, never renumbered: the snapshot and the wire store the int.</para>
        /// </summary>
        Spellbook = 8,

        /// <summary>
        /// Milestone 9.30b. The spells removed from the spellbook, lying faceup where every player
        /// can read them: "As the spell is cast, remove it from the spellbook faceup." One of p.4's
        /// open zones (<see cref="PublicCardZoneQuery"/>), and a card leaves it only by returning to
        /// <see cref="Spellbook"/>.
        /// </summary>
        RemovedFromSpellbook = 9
    }
}
