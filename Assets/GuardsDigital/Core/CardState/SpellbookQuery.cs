using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 9.30c. The three readings of a hero's spellbook that casting and refilling ask, stated
    /// once so the offer, the resolution and every other reader agree on them.
    ///
    /// <para><b>A spell is found by its definition, never by a card a chooser named.</b> Card text
    /// names a spell ("Cast the Burning Hands spell"), and every spell is one instance its owner holds
    /// (9.30b, GY-Q1), so the definition and the owner fix the instance. A decision about a spell
    /// therefore never has to carry a card identity from a facedown book to a seat that may not read
    /// it (GY-05: "choose the spells by name, without looking at them first").</para>
    ///
    /// <para><b>The two spellbook locations are never a hand, deck or discard</b>, and
    /// <see cref="IsInASpellbook"/> is how a reader that lists a hero's cards without naming a
    /// location says so (the 9.30c audit).</para>
    /// </summary>
    public static class SpellbookQuery
    {
        /// <summary>Whether a card lying in <paramref name="location"/> is a spell in the book or removed from it.</summary>
        public static bool IsInASpellbook(CardLocation location) =>
            location == CardLocation.Spellbook || location == CardLocation.RemovedFromSpellbook;

        /// <summary>
        /// <paramref name="ownerId"/>'s instance of the spell <paramref name="spellId"/> while it lies
        /// in the book, or null - the spell is removed, the hero owns no such spell, or no hero is
        /// named. One instance per definition (9.30b), so the lowest identity settles a malformed
        /// board deterministically rather than by collection order.
        /// </summary>
        public static CardInstance InBook(
            IEnumerable<CardInstance> instances, HeroId ownerId, CardDefinitionId spellId)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!ownerId.IsDefined || !spellId.IsDefined)
            {
                return null;
            }

            return instances
                .Where(card => card != null &&
                    card.OwnerId.Equals(ownerId) &&
                    card.State.Location == CardLocation.Spellbook &&
                    card.DefinitionReference.DefinitionId.Equals(spellId))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        /// <summary>Every spell <paramref name="ownerId"/> has removed from the book, in identity order.</summary>
        public static IReadOnlyList<CardInstance> Removed(
            IEnumerable<CardInstance> instances, HeroId ownerId)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!ownerId.IsDefined)
            {
                return Array.Empty<CardInstance>();
            }

            return Array.AsReadOnly(instances
                .Where(card => card != null &&
                    card.OwnerId.Equals(ownerId) &&
                    card.State.Location == CardLocation.RemovedFromSpellbook)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray());
        }
    }
}
