using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.71a. One hero's UNRESOLVED card - rulebook p.10's "a played card is unresolved
    /// until after you perform one of its actions" - asked of a whole card collection.
    ///
    /// <para><b>Why the reading is written down once.</b> Hanu the Trickster's Hurry up! prints "Set
    /// the printed [Initiative] Initiative value of an unresolved card of a hero in range to 11", and
    /// that phrase is asked THREE times about the same card: by the selection, which must not offer a
    /// hero holding no such card; by the body, which has to find the card the answer named; and by
    /// p.7's turn order, which reads exactly these cards and no others. A predicate spelled three
    /// times is three chances for a hero to be offered and then refused, which is milestone 8.35l's
    /// rule, so all three ask here and this asks
    /// <see cref="CardLifecycleAuthority.IsResolvingCardInTurnArea"/> - the shape milestone 8.69b
    /// already named for Brynn's Peak Precision, and the shape
    /// <c>InitiativeResolutionAuthority</c> orders the round by.</para>
    ///
    /// <para><b>A hero holds at most one.</b> p.10 commits one card per hero per round and an
    /// unresolved card is one that has been played and not yet acted on, so the singular in the
    /// printed sentence is the rulebook's own. Nothing here depends on that: the query answers about
    /// the FIRST such card in a stable order, so a composition that somehow held two would be
    /// answered deterministically rather than differently on each derivation - which is what a
    /// restore, a replay and a rewind all require of it.</para>
    ///
    /// <para><b>It reads cards and nothing else.</b> Whose side the hero is on, whether they are in
    /// range, and whether they are immune are the selection's questions; this one is about p.10.</para>
    /// </summary>
    public static class HeroUnresolvedCardQuery
    {
        /// <summary>Whether this hero currently holds a played, face-up, unresolved card.</summary>
        public static bool Holds(IEnumerable<CardInstance> cards, HeroId ownerId) =>
            TryFind(cards, ownerId, out _);

        /// <summary>
        /// That card, or nothing. The order is the card instance id's, so two derivations of the same
        /// collection answer identically however the collection was assembled.
        /// </summary>
        public static bool TryFind(
            IEnumerable<CardInstance> cards, HeroId ownerId, out CardInstance card)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            card = ownerId.IsDefined
                ? cards
                    .Where(value => value != null && value.OwnerId.Equals(ownerId))
                    .Where(CardLifecycleAuthority.IsResolvingCardInTurnArea)
                    .OrderBy(value => value.Id.Value, StringComparer.Ordinal)
                    .FirstOrDefault()
                : null;
            return card != null;
        }
    }
}
