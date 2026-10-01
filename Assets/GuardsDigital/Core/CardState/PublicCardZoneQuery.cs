namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.40f. Which of rulebook p.4's zones are OPEN - the places a card standing face up
    /// may be read by every player at the table.
    ///
    /// <para><b>Why the list is a derivation rather than a literal in each reader.</b> Two readers now
    /// ask it. <c>SeatProjection.IsPublic</c> asks it to decide whether another hero's card may be
    /// named to a seat, and <c>name-color@1</c>'s guess-domain narrowing asks it to subtract the cards
    /// public information already accounts for from a hero's printed deck. Those two must give the
    /// same answer or the second one leaks: a zone one of them called open and the other did not would
    /// let a guess domain rule out a colour no seat can actually see, which is the exact inference the
    /// clause exists to prevent.</para>
    ///
    /// <para><b>It states the OPEN half only, and the closed half stays fail-closed where it is.</b>
    /// p.4 makes "cards in the deck", "cards in hand" and "any facedown cards" hidden;
    /// <c>IsPublic</c>'s switch still ends in <c>default: return false</c>, so a location nobody has
    /// considered is withheld rather than published. This query adds no zone and removes none - it is
    /// the same four members, written down once. Milestone 9.30b adds a fifth, the spells removed from
    /// a spellbook faceup; the spellbook itself stays closed.</para>
    ///
    /// <para><b>Set aside is deliberately NOT open.</b> Rulebook p.15 tucks the untaken card of an
    /// upgrade pair under the dashboard facedown, so its identity is not readable and it falls through
    /// to the closed half. Everyone can see that a card went there; nobody can see which.</para>
    /// </summary>
    public static class PublicCardZoneQuery
    {
        /// <summary>
        /// Whether a card lying in <paramref name="location"/> is in one of p.4's open zones. It says
        /// nothing about face orientation - see <see cref="IsPubliclyVisible"/>, which asks both.
        /// </summary>
        public static bool IsAnOpenZone(CardLocation location)
        {
            switch (location)
            {
                case CardLocation.TurnArea:
                case CardLocation.DiscardPile:
                case CardLocation.ItemSlot:
                case CardLocation.UltimateArea:
                // Milestone 9.30b. "Remove it from the spellbook faceup": the removed pile is read by
                // every player, which is what lets a defender see the cast spell's Attack. The book
                // itself is not open - its cards are facedown and its owner's alone.
                case CardLocation.RemovedFromSpellbook:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether this card is one every player may currently read: face up, in an open zone.
        /// </summary>
        public static bool IsPubliclyVisible(CardInstance card) =>
            card != null &&
            card.State.FaceOrientation == CardFaceOrientation.FaceUp &&
            IsAnOpenZone(card.State.Location);
    }
}
