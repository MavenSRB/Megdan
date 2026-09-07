using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.35z45. Whether a named hero has anything in their discard pile.
    ///
    /// <para><b>Why this exists as a query rather than as a line of LINQ.</b> Three places in the build
    /// already asked it, each with its own copy of the same two clauses - the owner is this hero and the
    /// card is in <see cref="CardLocation.DiscardPile"/>. One of those copies carries a comment saying
    /// it is written "the same way <c>owned-card-present@1</c> reads one so a card that selects by the
    /// fact and a card that conditions on it cannot come to disagree about what it means", which is a
    /// statement three separate copies cannot actually keep. This milestone adds a FOURTH consumer - the
    /// per-candidate narrowing on a modal Attack's bullet - so the copy was made a derivation instead.
    /// The behaviour is unchanged in every existing caller; what changed is that there is now one place
    /// for the reading to be wrong in.</para>
    ///
    /// <para><b>What "the discard" means, and why nothing here has to know it.</b> Rulebook p.10 returns
    /// every card to its owner's hand at the end of a round, so the pile this reads is the current
    /// round's: heroes who have defended, or who have been made to discard. That falls out of reading
    /// live card state and is deliberately not encoded here - a query that knew about round boundaries
    /// would be a second place the lifecycle could be described.</para>
    ///
    /// <para><b>A hero nobody names holds nothing.</b> An undefined <see cref="HeroId"/> is answered
    /// <c>false</c> rather than refused, because every caller reaches this with a candidate it has
    /// already decided is a hero, and an undefined id there means "no hero", which owns no cards. The
    /// absence of card state is a different question entirely and is refused by
    /// <c>EffectCardState</c> at the caller, which is where the distinction belongs.</para>
    /// </summary>
    public static class HeroDiscardPileQuery
    {
        /// <summary>
        /// Whether <paramref name="ownerId"/> owns at least one card standing in the discard pile.
        /// </summary>
        public static bool HasAnyCard(IEnumerable<CardInstance> instances, HeroId ownerId)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            return ownerId.IsDefined && instances.Any(card =>
                card != null &&
                card.OwnerId.Equals(ownerId) &&
                card.State.Location == CardLocation.DiscardPile);
        }
    }
}
