using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.40f. How many cards a named hero is holding.
    ///
    /// <para><b>It is the count the table can already see, and that is the whole reason it may be
    /// asked.</b> <c>SeatProjection.SeatHandSummary</c> has published every hero's hand and deck
    /// COUNT to every seat since milestone 8.30a - "cards are dealt, played and discarded in the
    /// open, so anyone can count them - while the identities behind those counts are not" - and it
    /// crosses the wire on every seat frame. So a printed clause that narrows a selection by hand
    /// size discloses nothing a client could not already derive, and any client can check the
    /// greying for itself. A clause that read which COLOURS or which CARDS were in that hand would
    /// be a different question with a different answer; this one is not.</para>
    ///
    /// <para><b>One derivation, for the reason <see cref="HeroDiscardPileQuery"/> is one.</b> The
    /// seat summary and the selection narrowing must agree about what "cards in hand" means, and two
    /// copies of the same two clauses cannot keep that promise. Both ask here.</para>
    ///
    /// <para><b>A hero nobody names holds nothing.</b> An undefined <see cref="HeroId"/> counts zero
    /// rather than being refused, exactly as the discard query answers <c>false</c>, because every
    /// caller reaches this with a candidate it has already decided is a hero.</para>
    /// </summary>
    public static class HeroHandQuery
    {
        /// <summary>
        /// The number of cards <paramref name="ownerId"/> owns standing in
        /// <see cref="CardLocation.Hand"/>.
        /// </summary>
        public static int Count(IEnumerable<CardInstance> instances, HeroId ownerId)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            return !ownerId.IsDefined
                ? 0
                : instances.Count(card =>
                    card != null &&
                    card.OwnerId.Equals(ownerId) &&
                    card.State.Location == CardLocation.Hand);
        }

        /// <summary>
        /// Whether <paramref name="ownerId"/> holds at least <paramref name="minimum"/> cards. A
        /// minimum of zero or less is no narrowing at all and is answered <c>true</c> for every
        /// hero, which is what an unbound optional parameter means.
        /// </summary>
        public static bool HasAtLeast(
            IEnumerable<CardInstance> instances, HeroId ownerId, int minimum) =>
            minimum <= 0 || Count(instances, ownerId) >= minimum;
    }
}
